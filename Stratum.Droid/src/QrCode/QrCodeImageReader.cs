// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Java.Nio;
using Stratum.Droid.Util;
using ZXing;
using ZXing.Common;
using Uri = Android.Net.Uri;

namespace Stratum.Droid.QrCode
{
    public static class QrCodeImageReader
    {
        public static async Task<string> ScanImageFromFileAsync(Context context, Uri uri)
        {
            Bitmap bitmap;
            
            try
            {
                var data = await FileUtil.ReadFileAsync(context, uri);
                bitmap = await BitmapFactory.DecodeByteArrayAsync(data, 0, data.Length);
            }
            catch (Exception e)
            {
                throw new IOException("Failed to read file", e);
            }
            
            if (bitmap == null)
            {
                throw new IOException("Failed to decode bitmap");
            }
            
            var reader = new BarcodeReader<Bitmap>(null, null, ls => new HybridBinarizer(ls))
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                    TryHarder = true,
                    TryInverted = true
                }
            };
            
            using var buffer = ByteBuffer.Allocate(bitmap.ByteCount);
            await bitmap.CopyPixelsToBufferAsync(buffer);
            buffer.Rewind();
            
            var bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);
            
            var source = new RGBLuminanceSource(bytes, bitmap.Width, bitmap.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
            var result = await Task.Run(() => reader.Decode(source));
            
            return result?.Text;
        }
    }
}