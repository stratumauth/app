using System;
using AndroidX.Activity;

namespace Stratum.Droid.Callback
{
    public class BackPressCallback : OnBackPressedCallback
    {
        public BackPressCallback(bool enabled) : base(enabled)
        {
            Enabled = enabled;
        }

        public event EventHandler BackPressed;

        public override void HandleOnBackPressed()
        {
            BackPressed?.Invoke(this, EventArgs.Empty);
        }
    }
}