using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Interop;
using System.IO;
using System.Windows;

namespace SearchNow {
    public static class Extensions {
        public static async void DoAfter(this Action action, TimeSpan delay) {
            await Task.Delay(delay);
            action();
        }

        public static ImageSource ToImageSource(this Icon icon) {
            using (MemoryStream iconStream = new MemoryStream()) { 
                icon.Save(iconStream);
                iconStream.Seek(0, SeekOrigin.Begin);
                return (ImageSource)System.Windows.Media.Imaging.BitmapFrame.Create(iconStream);
            }
        }


        public static bool IsWithinRange(this int integer, int bottom, int top) {
            return (integer >= bottom && integer < top);
        }

    }
}
