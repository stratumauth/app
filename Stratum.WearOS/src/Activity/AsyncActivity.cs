// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Android.OS;
using AndroidX.AppCompat.App;
using DotNext.Threading;

namespace Stratum.WearOS.Activity
{
    public abstract class AsyncActivity : AppCompatActivity
    {
        private readonly AsyncManualResetEvent _initEvent = new(false);

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            await OnCreateAsync();
            _initEvent.Set();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            await _initEvent.WaitAsync();

            if (IsLaunchCancelled())
            {
                return;
            }

            await OnResumeAsync();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _initEvent.Set();
        }

        protected void RunOnUiThreadForLaunch(Action action)
        {
            if (!IsLaunchCancelled())
            {
                action();
            }
        }

        private bool IsLaunchCancelled()
        {
            return IsFinishing || IsDestroyed;
        }

        protected abstract Task OnCreateAsync();
        protected abstract Task OnResumeAsync();
    }
}