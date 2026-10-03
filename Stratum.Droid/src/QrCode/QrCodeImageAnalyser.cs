// Copyright (C) 2024 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Util;
using AndroidX.Camera.Core;
using Stratum.ZXing;
using Serilog;
using ImageFormat = Stratum.ZXing.ImageFormat;
using Log = Serilog.Log;

namespace Stratum.Droid.QrCode
{
    public class QrCodeImageAnalyser : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        public event EventHandler<string> QrCodeScanned;
        public Size DefaultTargetResolution => new(1280, 720);
        
        private readonly ILogger _log = Log.ForContext<QrCodeImageAnalyser>();

        private readonly QrCodeReader _qrCodeReader = new(new ReaderOptions
        {
            TryHarder = true,
            TryInvert = true,
            TryDownscale = true,
            Binarizer = Binarizer.GlobalHistogram
        });

        private byte[] _buffer = [];
        
        public void Analyze(IImageProxy imageProxy)
        {
            if (imageProxy.Image == null)
            {
                imageProxy.Close();
                return;
            }

            try
            {
                AnalyseInternal(imageProxy);
            }
            finally
            {
                imageProxy.Close();
            }
        }

        private void AnalyseInternal(IImageProxy imageProxy)
        {
            using var lumPlane = imageProxy.Image.GetPlanes()[0];
            
            lumPlane.Buffer.Rewind();

            if (lumPlane.Buffer.Remaining() != _buffer.Length)
            {
                _buffer = new byte[lumPlane.Buffer.Remaining()];
            }
            
            lumPlane.Buffer.Get(_buffer);
            
            using var imageView = new ImageView(_buffer, imageProxy.Width, imageProxy.Height, ImageFormat.Lum, lumPlane.RowStride, lumPlane.PixelStride);
            imageView.Crop(imageProxy.CropRect.Left, imageProxy.CropRect.Top, imageProxy.CropRect.Width(), imageProxy.CropRect.Height());
            imageView.Rotate(imageProxy.ImageInfo.RotationDegrees);
            
            string result;
            
            try
            {
                result = _qrCodeReader.Read(imageView);
            }
            catch (QrCodeException e)
            {
                _log.Warning(e, "Error scanning QR code: {Type}", e.Type);
                return;
            }

            if (result != null)
            {
                QrCodeScanned?.Invoke(this, result);
            }
        }
    }
}
