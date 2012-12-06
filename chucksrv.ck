//chucksrv.ck
//run like: chuck chucksrv.ck

"/rjf/p,iifff" => string oscAddress;
1973 => int oscPort;
10 => int vMax;

JCRev reverb;
Gain dist;
Chorus chorus;
SawOsc voices[vMax];
SawOsc voicesHi[vMax];

for( 0 => int vIdx; vIdx < vMax; vIdx++) {
  voices[vIdx] => dist;
  voicesHi[vIdx] => dist;

  0 => voices[vIdx].gain;
  440 => voices[vIdx].freq;

  0 => voicesHi[vIdx].gain;
  880 => voicesHi[vIdx].freq;
}

dist => Gain g3 => dist;
0.75 => g3.gain;

0.75 => dist.gain;
0.5 => reverb.mix;
dist => chorus;
chorus => reverb;
reverb => dac;

0.025125 => float initialvol;
initialvol => float overallvol;
8 => int octdiff;

OscRecv recv;
oscPort => recv.port;
recv.listen();
recv.event( oscAddress ) @=> OscEvent oe;

while( oe => now ) {
  if( oe.nextMsg() ) {
    oe.getInt() => int timediff;
    oe.getInt() => int voice;
    oe.getFloat() => float gain;
    oe.getFloat() => float freq; 
    oe.getFloat() => float timbre;

    if(voice >= 0)
    {
      octdiff * 0.125 * freq => voices[voice].freq;
      octdiff * 0.25 * freq => voicesHi[voice].freq;


      gain * overallvol * timbre     => voices[voice].gain;
      gain * overallvol * (1-timbre) => voicesHi[voice].gain;
    }
    else {
      //-1 signifies a volume change
      if(voice == -1) {
        gain * 0.01 * initialvol => overallvol;
      }
      //-2 signifies an octave base change
      if(voice == -2) {
        for(1 => octdiff; gain >= 0; gain - 20 => gain)
        {
          octdiff * 2 => octdiff;
        }
      }
      if(voice == -3) {
        gain * 0.01 => reverb.mix;
      }
      if(voice == -4) {
        gain * 0.005 => chorus.modFreq;
      }
      if(voice == -5) {
        gain * 0.01 => chorus.modDepth;
      }
      if(voice == -6) {
        gain * 0.01 => chorus.mix;
      }
    }
    
    //<<< "voice=", voice, ",vol=", gain, ",freq=", freq, ",timbre", timbre >>>;
  }
  me.yield();
}

while( true ) {
  1::second => now;
  me.yield();
}



