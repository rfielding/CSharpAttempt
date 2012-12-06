//chuckcli.ck
//run like:  chuck chuckcli.ck

"/rjf/p,iifff" => string oscAddress;
1973 => int oscPort;
10 => int vMax;
"127.0.0.1" => string oscHost;

OscSend xmit;
float freq[vMax];
float vol[vMax];

for( 0 => int vIdx; vIdx < vMax; vIdx++ ) {
  220 => freq[vIdx];
  0.0 => vol[vIdx];
}


[1.0, 9.0/8, 6.0/5, 4.0/3, 3.0/2] @=> float baseNotes[];
float baseShift[vMax];
int noteIndex[vMax];
0 => int timediff;
for( 0 => int vIdx; vIdx < vMax; vIdx++ ) {
  1.0 => baseShift[vIdx];
  0 => noteIndex[vIdx];
}

xmit.setHost(oscHost,oscPort);

while( true )
{
  Std.rand2(0,vMax-1) => int voice;

  //(((Std.rand2(0,255) / 256.0)*1.0-0.5)*0.1*freq[voice] + freq[voice]) => freq[voice];
  ((1.0+((Std.rand2(0,255) / 256.0)*1.0-0.5)*0.0025)*baseShift[voice]) => baseShift[voice];

  //Maybe follow leader
  if(Std.rand2(0,256) < 1) {
    0 => noteIndex[1];
    noteIndex[1] => noteIndex[voice];
    baseShift[1] => baseShift[voice];
  }
  if(Std.rand2(0,256) < 1) {
    0 => noteIndex[0];
    noteIndex[0] => noteIndex[voice];
    baseShift[0] => baseShift[voice];
  }
  //Stay in range
  if(vol[voice] < 0) {
    0 => vol[voice];
  }
  if(vol[voice] > 1) {
    1 => vol[voice];
  }
  if(baseShift[voice] < 1) {
    baseShift[voice] * 2.0 => baseShift[voice];
  }
  if(baseShift[voice] > 32) {
    baseShift[voice] * 0.5 => baseShift[voice];
  }
  //Maybe silent
  if(Std.rand2(0,64) < 1) {
    0 => vol[voice];
  }
  if(Std.rand2(0,3) < 2) {
    0.01 +=> vol[voice];
  }
  if(Std.rand2(0,1) < 1) {
    0.005 -=> vol[voice];
  }
  //Octave jumps
  if(Std.rand2(0,4) < 1) {
    baseShift[voice] * 2.0 => baseShift[voice];
  }
  if(Std.rand2(0,4) < 1) {
    baseShift[voice] * 0.5 => baseShift[voice];
  }
  //Fifth jumps
  if(Std.rand2(0,256) < 1) {
    baseShift[voice] * 3.0/2 => baseShift[voice];
  }
  if(Std.rand2(0,256) < 1) {
    baseShift[voice] * 2.0/3 => baseShift[voice];
  }
  //Walk scale
  if(Std.rand2(0,8) < 1) {
    0 => noteIndex[voice];
  }
  if(Std.rand2(0,16) < 1) {
    (noteIndex[voice] + 1) % 5 => noteIndex[voice];
  }
  if(Std.rand2(0,16) < 1) {
    (noteIndex[voice] - 1 + vMax) % 5 => noteIndex[voice];
  }
  //Make freq
  27.5 * baseShift[voice] * baseNotes[noteIndex[voice]] => freq[voice];

  xmit.startMsg(oscAddress);
  xmit.addInt(timediff);
  xmit.addInt(voice);
  xmit.addFloat(vol[voice]);
  xmit.addFloat(freq[voice]);
  xmit.addFloat(1.0);

  35::ms +=> now;
  <<< "voice=", voice, ",vol=", vol[voice], ",freq=", freq[voice] >>>;
}
