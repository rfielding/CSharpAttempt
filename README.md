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

* Run chucksrv.ck clean before starting up this program.
When it starts, press the "Connect" button.
Now when you touch the screen, OSC packets will be sent to the UDP port.

A video on doing this is here:

http://www.youtube.com/watch?v=6hgCIeF7inM

