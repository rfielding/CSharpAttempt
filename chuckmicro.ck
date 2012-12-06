//Run this with ChucK like:  chuck [filename]
SawOsc s => Gain g => Pan2 p => JCRev r => dac;

.1 => s.gain;
.2 => g.gain;
.9 => r.mix;
.5 => p.pan;

[ 1.0, 11.0/10, 6.0/5, 4.0/3, 3.0/2 ]@=> float nt[];

while( true )
{
  (p.pan() + Std.rand2(0,8))/8 => p.pan;
  ( 27.5 * 2.0*Std.rand2(0,2) * 3.0/2*(Std.rand2(0,4)-2) * nt[Std.rand2(0,nt.cap()-1)] ) => s.freq;
  130::ms => now;
}