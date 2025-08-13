// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Graphics;
using Google.Android.Material.Button;
using Java.Util.Concurrent;
using Stratum.Droid.QrCode;

namespace Stratum.Droid.Activity
{
    [Activity(EnableOnBackInvokedCallback = true)]
    public class ScanActivity : BaseActivity
    {
        private PreviewView _previewView;
        private ICamera _camera;
        private bool _isFlashOn;

        public ScanActivity() : base(Resource.Layout.activityScan)
        {
        }

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            {
                Toast.MakeText(this, "Camera is not supported on this version of Android.", ToastLength.Long).Show();
                Finish();
                return;
            }

            _previewView = FindViewById<PreviewView>(Resource.Id.previewView);
            _previewView.Touch += OnPreviewViewTouch;

            var flashButton = FindViewById<MaterialButton>(Resource.Id.buttonFlash);
            flashButton.Click += OnFlashButtonClick;

            var provider = (ProcessCameraProvider) await ProcessCameraProvider.GetInstance(this).GetAsync();
            var preview = new Preview.Builder().Build();
            
            var cameraSelector = new CameraSelector.Builder()
                .RequireLensFacing(CameraSelector.LensFacingBack)
                .Build();

            var executor = Executors.NewCachedThreadPool();
            preview.SetSurfaceProvider(executor, _previewView.SurfaceProvider);

            var resolutionSelector = new ResolutionSelector.Builder()
                .SetAspectRatioStrategy(AspectRatioStrategy.Ratio169FallbackAutoStrategy)
                .SetAllowedResolutionMode(ResolutionSelector.PreferCaptureRateOverHigherResolution)
                .Build();

            var analysis = new ImageAnalysis.Builder()
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                .SetOutputImageFormat(ImageAnalysis.OutputImageFormatYuv420888)
                .SetResolutionSelector(resolutionSelector)
                .Build();
            
            var analyser = new QrCodeImageAnalyser();
            analyser.QrCodeScanned += OnQrCodeScanned;
            analysis.SetAnalyzer(executor, analyser);

            _camera = provider.BindToLifecycle(this, cameraSelector, analysis, preview);
        }

        private void OnPreviewViewTouch(object sender, View.TouchEventArgs args)
        {
            if (args.Event?.Action != MotionEventActions.Up)
            {
                return;
            }

            var factory = new SurfaceOrientedMeteringPointFactory(_previewView.Width, _previewView.Height);
            var point = factory.CreatePoint(args.Event.GetX(), args.Event.GetY());

            var action = new FocusMeteringAction.Builder(point, FocusMeteringAction.FlagAf)
                .DisableAutoCancel()
                .Build();

            _camera.CameraControl.StartFocusAndMetering(action);
        }

        private void OnFlashButtonClick(object sender, EventArgs e)
        {
            _isFlashOn = !_isFlashOn;
            _camera.CameraControl.EnableTorch(_isFlashOn);
        }

        private void OnQrCodeScanned(object sender, string qrCode)
        {
            var intent = new Intent();
            intent.PutExtra("text", qrCode);
            SetResult(Result.Ok, intent);
            Finish();
        }

        protected override void OnApplySystemBarInsets(Insets insets)
        {
            var topLayout = FindViewById<LinearLayout>(Resource.Id.topLayout);
            topLayout.SetPadding(topLayout.PaddingLeft, topLayout.PaddingTop + insets.Top, topLayout.PaddingRight,
                topLayout.PaddingBottom);

            var bottomLayout = FindViewById<LinearLayout>(Resource.Id.bottomLayout);
            bottomLayout.SetPadding(bottomLayout.PaddingLeft, bottomLayout.PaddingTop, bottomLayout.PaddingRight,
                bottomLayout.PaddingBottom + insets.Bottom);
        }
    }
}