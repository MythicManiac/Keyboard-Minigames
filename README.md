Snake Game
=============

A snake game for the Corsair keyboards for Windows, written in C# .Net.

This project has been tested with the following keyboards:
* Corsair K70 RGB

Other Corsair keyboards technically should work as well, however, I have not been able to test them

What is it?
----------

This is a simple snake game written in C# with Corsair's CUE SDK's help

Basically it turns your keyboard into a snake game, see example here: https://www.youtube.com/watch?v=yYet1X18F2E&feature=youtu.be

How to use
----------

1. Download the latest version from the **RELEASES** section (not the "Download ZIP" button)
2. Exctract the zip somewhere
3. Open your Corsair Utility Engine
4. Select "Assign New Action" on the button you wish to use to launch the snake game
5. Choose "Shortcut"
6. Choose "Run the following program"
7. Navigate to the directory you extracted the zip, and select the "SnakeGame" file

Now you can start the snake game by pressing your macro key.

To stop the game, hit esc

Download
--------

You can find all releases under the "releases" tab on github.

Alternatively, you can clone the project and compile it yourself.

Credits
-------
For making this project possible, I'd like to thank
* DarthAffe for providing a .NET wrapped version of Corsair's CUE SDK: https://github.com/DarthAffe/CUE.NET
* Corsair for producing some sweet keyboards

For inspiring me to write the original code for this project and making it possible, I'd like to thank
* CalcProgrammer1 for reverse engineering the USB IO for this keyboard and for providing working C++ code. See: http://www.reddit.com/r/MechanicalKeyboards/comments/2ij2um/corsair_k70_rgb_usb_protocol_reverse_engineering/
* Billism1 for providing me a good working example of interfacing with the keyboard. See: https://github.com/billism1/KeyboardAudio
