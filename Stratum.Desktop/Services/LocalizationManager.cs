// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using System.Windows;
using Serilog;

namespace Stratum.Desktop.Services
{
    public enum AppLanguage
    {
        English,
        Chinese
    }

    public class LocalizationManager
    {
        private static readonly string EnglishResourcePath = "Resources/Strings.en.xaml";
        private static readonly string ChineseResourcePath = "Resources/Strings.zh.xaml";
        private readonly ILogger _log = Log.ForContext<LocalizationManager>();

        public AppLanguage CurrentLanguage { get; private set; }

        public void SetLanguage(AppLanguage language)
        {
            _log.Information("Setting language to {Language}", language);

            var resourcePath = language switch
            {
                AppLanguage.Chinese => ChineseResourcePath,
                _ => EnglishResourcePath
            };

            var app = Application.Current;
            if (app == null)
            {
                _log.Warning("Application.Current is null, cannot set language");
                return;
            }

            var newDictionary = new ResourceDictionary
            {
                Source = new Uri(resourcePath, UriKind.Relative)
            };

            // Find and remove existing language dictionary
            var existingDict = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null &&
                    (d.Source.OriginalString.Contains("Strings.en.xaml") ||
                     d.Source.OriginalString.Contains("Strings.zh.xaml")));

            if (existingDict != null)
            {
                _log.Information("Removing existing language dictionary: {Path}", existingDict.Source?.OriginalString);
                app.Resources.MergedDictionaries.Remove(existingDict);
            }

            app.Resources.MergedDictionaries.Add(newDictionary);
            CurrentLanguage = language;
            _log.Information("Language set successfully to {Language} using {Path}", language, resourcePath);
        }

        public static string GetString(string key)
        {
            var value = Application.Current?.TryFindResource(key);
            return value as string ?? key;
        }
    }
}
