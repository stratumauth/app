// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace Stratum.Desktop.Services
{
    public class PreferenceManager
    {
        private const string FileName = "settings.json";
        private readonly ILogger _log = Log.ForContext<PreferenceManager>();
        private readonly string _filePath;
        private Preferences _preferences;

        public PreferenceManager()
        {
            _filePath = Path.Combine(App.GetDataDirectory(), FileName);
            Load();
        }

        public Preferences Preferences => _preferences;

        private void Load()
        {
            try
            {
                _log.Information("Loading preferences from {Path}", _filePath);

                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    var options = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }
                    };
                    _preferences = JsonSerializer.Deserialize<Preferences>(json, options) ?? new Preferences();
                    _log.Information("Preferences loaded successfully. Language: {Language}, Theme: {Theme}",
                        _preferences.Language, _preferences.Theme);
                }
                else
                {
                    _preferences = new Preferences();
                    _log.Information("Settings file not found, using default preferences. Language: {Language}",
                        _preferences.Language);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to load preferences, using defaults");
                _preferences = new Preferences();
            }
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var json = JsonSerializer.Serialize(_preferences, options);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _log.Information("Created settings directory: {Directory}", directory);
                }

                File.WriteAllText(_filePath, json);
                _log.Information("Preferences saved to {Path}. Language: {Language}, Theme: {Theme}",
                    _filePath, _preferences.Language, _preferences.Theme);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to save preferences to {Path}", _filePath);
            }
        }
    }

    public class Preferences
    {
        public Theme Theme { get; set; } = Theme.System;
        public AppLanguage Language { get; set; } = AppLanguage.English;
        public SortMode SortMode { get; set; } = SortMode.AlphabeticalAscending;
        public bool TapToCopy { get; set; } = true;
        public bool ShowUsernames { get; set; } = true;
        public int CodeGroupSize { get; set; } = 3;
        public int AutoLockTimeoutMinutes { get; set; } = 0;
        public string SelectedCategoryId { get; set; } = null;
        public bool MinimizeToTray { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public bool StartWithWindows { get; set; } = false;
    }

    public enum Theme
    {
        Light,
        Dark,
        System
    }

    public enum SortMode
    {
        AlphabeticalAscending,
        AlphabeticalDescending,
        CopyCountDescending,
        Custom
    }
}
