// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using AndroidX.Activity;

namespace Stratum.WearOS.Callback
{
    public class BackPressCallback(bool enabled) : OnBackPressedCallback(enabled)
    {
        public event EventHandler BackPressed;

        public override void HandleOnBackPressed()
        {
            BackPressed?.Invoke(this, EventArgs.Empty);
        }
    }
}
