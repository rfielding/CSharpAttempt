//chucksrv.ck
//run like: chuck chucksrv.ck

"/rjf/p,iifff" => string oscAddress;
1973 => int oscPort;
10 => int vMax;

JCRev reverb;
Gain dist;
SawOsc voices[vMax];
SawOsc voicesHi[vMax];
//SinOsc voicesLo[vMax];

for( 0 => int vIdx; vIdx < vMax; vIdx++) {
  voices[vIdx] => dist;
  voicesHi[vIdx] => dist;
  //voicesLo[vIdx] => dist;

  0 => voices[vIdx].gain;
  440 => voices[vIdx].freq;

  0 => voicesHi[vIdx].gain;
  880 => voicesHi[vIdx].freq;

  //0 => voicesLo[vIdx].gain;
  //220 => voicesLo[vIdx].freq;
}

//adc => Gain g1 => DelayL d => dac;
//adc => Gain g2 => dac;
dist => Gain g3 => dist;
0.75 => g3.gain;
//0.1 => g2.gain;
//0.1 => g1.gain;

0.75 => dist.gain;
0.75 => reverb.mix;
dist => reverb;
reverb => dac;


0.025125 => float overallvol;

OscRecv recv;
oscPort => recv.port;
recv.listen();
recv.event( oscAddress ) @=> OscEvent oe;

while( oe => now ) {
  if( oe.nextMsg() ) {
    oe.getInt() => int timediff;
    oe.getInt() => int voice;
    oe.getFloat() => float gain;

    oe.getFloat() => float freq => voices[voice].freq;
    2 * freq => voicesHi[voice].freq;
    //0.5 * freq => voicesLo[voice].freq;

    oe.getFloat() => float timbre;

    gain * overallvol * timbre     => voices[voice].gain;
    gain * overallvol * (1-timbre) => voicesHi[voice].gain;

//    if(timbre <= 0.5) {
//      0 => voicesHi[voice].gain;
//      gain * overallvol * timbre * 0.5 => voices[voice].gain;
//      gain * overallvol * (1 - timbre * 0.5) => voicesLo[voice].gain;
//    }
//    else {
//      gain * overallvol * (1 - (timbre - 0.5) * 0.5) => voicesHi[voice].gain;
//      gain * overallvol * (timbre - 0.5) * 0.5 => voices[voice].gain;
//      0 => voicesLo[voice].gain;
//    }
    
    //<<< "voice=", voice, ",vol=", gain, ",freq=", freq, ",timbre", timbre >>>;
  }
  //me.yield();
}

while( true ) {
  1::second => now;
  me.yield();
}



