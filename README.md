CSharpAttempt
=============

This is an OSC Controller for driving ChucK or SuperCollider for Windows8.
It has no sound engine at all.
It just sends OSC formatted packets over UDP.
Basically, OSC is a trivial RPC protocol for sending raw structures of ints and floats.
It is similar to apps like Mugician, GeoSynthesizer, and Cantor for iOS.
The main layout should be familiar to guitarists because it is rows of chromatics rising by a fourth.

* First, an OSC runtime such as ChucK or SuperCollider is required to make any use of this.  
I used ChucK 1.3.1.3 executable for my own testing, and ran it from the command-line.

* By default the test server script chucksrv.ck happens to run on the same UDP port that SuperCollider would expect to get OSC messages on.
This means that if you start and run a SuperCollider server, then it can work with this instrument.
(I did not include the SuperCollider script I used to test this in this repo; just the ChucK versions).
Dont run this script at the same time as SuperCollider (use SuperCollider *instead* of this script),
as they use the same UDP port to listen for incoming data.

* The pair of scripts chucksrv.ck should be run first.  
The the script chuckcli.ck can also be run in a different window to smoke-test ChucK itself.
The client is a Just Intonation noise-maker script that will slowly start playing sound when it is run.
If you are interested in algorithmic music with ChucK, then play around with these two files for a while
before trying to build this code.

* Run chucksrv.ck clean before starting up this program.
When it starts, press the "Connect" button.
Now when you touch the screen, OSC packets will be sent to the UDP port.

A video on doing this is here:

http://www.youtube.com/watch?v=6hgCIeF7inM

The Instrument
==============

The main points about this instrument:

* Ergonomics allow you to play with guitarist skills.  It is laid out as rows of fourths.
* You should be able to play with about the same speed and skill of a real guitar, other than latency effects of your hardware and synth combination
* Hammer-on/Hammer-off per row, similar to a normal string instrument.  This is the most crucial element to fast and realistic play for the instrument.
* You can replace the server script with anything that accepts OSC messages.  If you need to modify the OSC messages to match a different system (ie: CSound), then you can edit the source as well.
* Not including a sound engine allows this source code to be in C#.  If you attempt to embed an audio engine, you may find that you need to write it in C++ to get deterministic timing.  Making this only send UDP packets reduces the real-time burden for the interface.
* I have tested this only with a Lenovo Ideacentre A7.  I haven't ported this to WinRT or Surface, because having to talk to a separate OSC process is a "non-starter" for the app store.
* You can play fretlessly, as a violinist or oud player would.  The ability to switch into this mode is rather the whole point of this instrument.  It is why MIDI is such a bad fit for it.
