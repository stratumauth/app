// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Serilog;

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
            if (string.IsNullOrEmpty(authenticator.Icon))
            {
                return GetDefaultIcon();
            }

            if (authenticator.Icon.StartsWith(CustomIcon.Prefix.ToString()))
            {
                return GetCustomIcon(authenticator.Icon.Substring(1));
            }

            return GetBuiltInIcon(authenticator.Icon);
        }

        private ImageSource GetCustomIcon(string iconId)
        {
            if (_cache.TryGetValue(iconId, out var cached))
            {
                return cached;
            }

            try
            {
                var customIcon = _customIconRepository.GetAsync(iconId).GetAwaiter().GetResult();
                if (customIcon?.Data != null)
                {
                    var image = new BitmapImage();
                    using (var ms = new MemoryStream(customIcon.Data))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = ms;
                        image.EndInit();
                        image.Freeze();
                    }
                    _cache[iconId] = image;
                    return image;
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to load custom icon {IconId}", iconId);
            }

            return GetDefaultIcon();
        }

        private ImageSource GetBuiltInIcon(string iconName)
        {
            if (_cache.TryGetValue(iconName, out var cached))
            {
                return cached;
            }

            try
            {
                var uri = new Uri($"pack://application:,,,/Assets/Icons/{iconName}.png", UriKind.Absolute);
                var image = new BitmapImage(uri);
                image.Freeze();
                _cache[iconName] = image;
                return image;
            }
            catch (Exception ex)
            {
                _log.Debug("Built-in icon not found: {IconName}, {Error}", iconName, ex.Message);
            }

            return GetDefaultIcon();
        }

        private ImageSource GetDefaultIcon()
        {
            const string key = "__default__";
            if (_cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            try
            {
                var uri = new Uri("pack://application:,,,/Assets/Icons/default.png", UriKind.Absolute);
                var image = new BitmapImage(uri);
                image.Freeze();
                _cache[key] = image;
                return image;
            }
            catch
            {
                // Return null if default icon doesn't exist
                return null;
            }
        }

        public void ClearCache()
        {
            _cache.Clear();
        }
    }
}
