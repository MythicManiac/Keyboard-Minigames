Snake Game
=============

A snake game for the Corsair K70 RGB keyboard for Windows, written in C# .Net.

While only tested on the K70 RGB, there's a high possibility of working with others as well.

This is based on the following C# project:
https://github.com/billism1/KeyboardAudio

What is it
----------

This is a simple snake game built on top of this project: https://github.com/billism1/KeyboardAudio

Basically it turns your keyboard into a snake game, see example here: https://www.youtube.com/watch?v=yYet1X18F2E&feature=youtu.be

How to use
----------

To start the game, turn off your Corsair Utility Engine, and launch the executable.

To stop the game, just close the window, or hit esc.

If your keyboard derps out and won't take input / lights are frozen, just unplug and replug the USB cables and it should work again.

Notes
-----
I never intended to release this at all, so the source might have some silly things lying all over the place.

If you choose to clone this and use for your own purposes, you might want to clean this up a bit.

Credits
-------
Thank you Billism1 for providing the base code of this project

Thank you CalcProgrammer1 for reverse engineering the USB IO for this keyboard and for providing working C++ code. See: http://www.reddit.com/r/MechanicalKeyboards/comments/2ij2um/corsair_k70_rgb_usb_protocol_reverse_engineering/ and thanks to reddit.com/u/fly-hard for the mapping of LED to positions in a matrix.

Thank you Chris Lomont for providing a C# Fast Fourier transform (FFT) implementation that is easy to use. See: http://www.lomont.org/

Thank you Corsair for producing a sweet keyboard.
