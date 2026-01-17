// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Stratum.Core;
using Stratum.Core.Entity;

namespace Stratum.Desktop.Services
{
    public class DesktopCustomIconDecoder : ICustomIconDecoder
    {
        public async Task<CustomIcon> DecodeAsync(byte[] data, bool shouldPreProcess)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Icon data cannot be empty");
            }

            byte[] processedData;

            if (shouldPreProcess)
            {
                processedData = await ProcessImageAsync(data);
            }
            else
            {
                processedData = data;
            }

            var hash = SHA1.HashData(processedData);
            var id = Convert.ToHexString(hash).ToLowerInvariant().Substring(0, 16);

            return new CustomIcon
            {
                Id = id,
                Data = processedData
            };
        }

        private static async Task<byte[]> ProcessImageAsync(byte[] data)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var ms = new MemoryStream(data);
                    var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    var frame = decoder.Frames[0];

                    // Resize if needed
                    var targetSize = CustomIcon.MaxSize;
                    BitmapSource resized;

                    if (frame.PixelWidth > targetSize || frame.PixelHeight > targetSize)
                    {
                        var scaleX = (double)targetSize / frame.PixelWidth;
                        var scaleY = (double)targetSize / frame.PixelHeight;
                        var scale = Math.Min(scaleX, scaleY);

                        resized = new TransformedBitmap(frame, new System.Windows.Media.ScaleTransform(scale, scale));
                    }
                    else
                    {
                        resized = frame;
                    }

                    // Encode to PNG
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(resized));

                    using var outputMs = new MemoryStream();
                    encoder.Save(outputMs);
                    return outputMs.ToArray();
                }
                catch
                {
                    // If processing fails, return original data
                    return data;
                }
            });
        }
    }
}
