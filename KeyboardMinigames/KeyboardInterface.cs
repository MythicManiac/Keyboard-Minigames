using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUE.NET;
using CUE.NET.Devices.Keyboard;
using CUE.NET.Devices.Keyboard.Enums;
using System.Drawing;

namespace KeyboardMinigames
{
    // CorsairKeyboard has no accessible constructors, so we'll just have to use a lame proxy class
    public class KeyboardInterface
    {
        public CorsairKeyboard Keyboard { get; private set; }

        public KeyboardInterface(CorsairKeyboard keyboard)
        {
            Keyboard = keyboard;
        }

        public void SetLed(int index, int r, int g, int b)
        {
            SetLed((CorsairKeyboardKeyId)index, r, g, b);
        }

        public void SetLed(CorsairKeyboardKeyId keyId, int r, int g, int b)
        {
            var key = Keyboard[keyId];
            if (key == null) { return; }
            key.Led.Color = Color.FromArgb(r, g, b);
        }

        public void Update()
        {
            Keyboard.Update();
        }
    }
}
