// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;

namespace Stratum.Droid.Activity
{
    public abstract class SensitiveSubActivity : BaseActivity
    {
        protected SensitiveSubActivity(int layout) : base(layout)
        {
        }

        protected override void OnResume()
        {
            base.OnResume();
            var session = Dependencies.Resolve<DatabaseSession>();

            if (!session.IsUnlocked)
            {
                Finish();
                return;
            }

            var windowFlags = !Preferences.AllowScreenshots ? WindowManagerFlags.Secure : 0;
            Window.SetFlags(windowFlags, windowFlags);
        }
    }
}