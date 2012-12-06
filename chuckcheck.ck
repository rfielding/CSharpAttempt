JCRev r;
SawOsc s1 => Gain g1 => Pan2 p1 => r => dac;
SawOsc s2 => Gain g2 => Pan2 p2 => r => dac;
dac => Gain gg1 => DelayL d => dac;
dac => Gain gg2 => dac;
d => Gain gg3 => d;

27.5::ms => d.delay;

.01 => gg1.gain;
.01 => gg2.gain;
.01 => gg3.gain;

.1 => s1.gain;
.1 => s1.gain;
.1 => g1.gain;
.5 => p1.pan;
.1 => s2.gain;
.1 => s2.gain;
.1 => g2.gain;
.5 => p2.pan;
.9 => r.mix;

//Just minor scale
[ 1.0, 9.0/8, 6.0/5, 4.0/3, 3.0/2, 2*4.0/5, 2*8.0/9 ] @=> float nt[];

while( true )
{
  (p1.pan() + Std.rand2(0,8))/8 => p1.pan;
  (p1.pan() + Std.rand2(0,8))/32 => p2.pan;
  ( 110 * 2.0*Std.rand2(0,3) * nt[Std.rand2(0,nt.cap()-1)] ) => s1.freq;
  ( s1.freq() * 1.0125) => s2.freq;
  130::ms +=> now;
}