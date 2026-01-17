// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Stratum.Core;

namespace Stratum.Desktop.Services
{
    public class DesktopAssetProvider : IAssetProvider
    {
        public async Task<byte[]> ReadBytesAsync(string path)
        {
            var uri = new Uri($"pack://application:,,,/{path}", UriKind.Absolute);

            try
            {
                var info = Application.GetResourceStream(uri);
                if (info == null)
                {
                    throw new FileNotFoundException($"Asset not found: {path}");
                }

                using var ms = new MemoryStream();
                await info.Stream.CopyToAsync(ms);
                return ms.ToArray();
            }
            catch (Exception)
            {
                // Try to read from file system as fallback
                var assemblyDir = AppContext.BaseDirectory;
                var filePath = Path.Combine(assemblyDir, path);

                if (File.Exists(filePath))
                {
                    return await File.ReadAllBytesAsync(filePath);
                }

                throw new FileNotFoundException($"Asset not found: {path}");
            }
        }

        public async Task<string> ReadStringAsync(string path)
        {
            var bytes = await ReadBytesAsync(path);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
