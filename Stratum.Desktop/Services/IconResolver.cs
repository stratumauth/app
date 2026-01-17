// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Serilog;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;

namespace Stratum.Desktop.Services
{
    public class IconResolver
    {
        private readonly ILogger _log = Log.ForContext<IconResolver>();
        private readonly ICustomIconRepository _customIconRepository;
        private readonly Dictionary<string, ImageSource> _cache = new();

        public IconResolver(ICustomIconRepository customIconRepository)
        {
            _customIconRepository = customIconRepository;
        }

        public ImageSource GetIcon(Authenticator authenticator)
        {
            if (_cache.TryGetValue("default", out var cached))
            {
                return cached;
            }

            var iconData = TryGetBuiltInIconBytes("default");
            if (iconData != null)
            {
                try
                {
                    var image = new BitmapImage();
                    using (var ms = new MemoryStream(iconData))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = ms;
                        image.EndInit();
                        image.Freeze();
                    }

                    _cache["default"] = image;
                    return image;
                }
                catch (Exception ex)
                {
                    _log.Debug("Failed to decode default icon: {Error}", ex.Message);
                }
            }

            return null;
        }

        private byte[] TryGetBuiltInIconBytes(string iconName)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Assets/Icons/{iconName}.png", UriKind.Absolute);
                var info = System.Windows.Application.GetResourceStream(uri);
                if (info == null)
                {
                    return null;
                }

                using var ms = new MemoryStream();
                info.Stream.CopyTo(ms);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        public void ClearCache()
        {
            _cache.Clear();
        }
    }
}
