Automatic Lipsync using SAPI 5.1

This console program and source code demonstrates how SAPI 5.1 can be
used to generate phoneme timings.

It supports two modes which we call "text based" and "textless".

In the "text based" mode, the program is given an audio file and a text transcript
of the audio file and trys to generate phoneme and word timings (alignments).

In the "textless" mode, the program is given only the audio file. It
will use ASR to generate word and phoneme timings. 

The output is printed to the console. This can be redirected to via ">" on the console
into a file. The format is actually an Annosoft .anno file. This is useful in that
it can be opened in "The Lipsync Tool" for viewing. A free version of the
software available at http://www.annosoft.com/demos.htm can be used.

Revision History

Oct 22 2005 Initial Release

