using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchNow {
    public static class ActionExtensions {
        public static async void DoAfter(this Action action, TimeSpan delay) {
            await Task.Delay(delay);
            action();
        }
    }
}
