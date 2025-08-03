// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.App;
using Android.Runtime;

namespace Stratum.WearOS
{
#if DEBUG
    [Application(Debuggable = true, TaskAffinity = "")]
#else
    [Application(Debuggable = false, TaskAffinity = "")]
#endif
    public class BaseApplication : Application
    {
        protected BaseApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            Dependencies.Init(this);
        }
    }
}
