// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Windows;
using Autofac;
using Serilog;
using Stratum.Desktop.Persistence;
using Stratum.Desktop.Services;

namespace Stratum.Desktop
{
    public partial class App : Application
    {
        public static IContainer Container { get; private set; }
        public static Database Database { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                InitializeLogging();
                Log.Information("=== Stratum Desktop Starting ===");
                Log.Information("Data directory: {DataDir}", GetDataDirectory());

                EnsureDataDirectory();

                SQLitePCL.Batteries_V2.Init();

                Database = new Database();
                Container = Dependencies.Build(Database);

                // Initialize localization
                Log.Information("Initializing localization...");
                var prefManager = Container.Resolve<PreferenceManager>();
                var locManager = Container.Resolve<LocalizationManager>();

                Log.Information("Loaded language preference: {Language}", prefManager.Preferences.Language);
                locManager.SetLanguage(prefManager.Preferences.Language);
                Log.Information("Language set to: {Language}", locManager.CurrentLanguage);

                await Database.OpenAsync(null, Database.Origin.Application);

                Log.Information("Creating main window...");
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Log.Information("Main window shown");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start application");
                MessageBox.Show(
                    $"Failed to start: {ex.Message}\n\n{ex.StackTrace}",
                    "Stratum",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Unhandled dispatcher exception");
            MessageBox.Show(
                $"Unhandled error: {e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "Stratum Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Log.Error(ex, "Unhandled domain exception");
            MessageBox.Show(
                $"Fatal error: {ex?.Message}\n\n{ex?.StackTrace}",
                "Stratum Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (Database != null)
            {
                await Database.CloseAsync(Database.Origin.Application);
            }

            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private static void InitializeLogging()
        {
            var logPath = Path.Combine(GetDataDirectory(), "logs", "stratum-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
#if DEBUG
                .WriteTo.Console()
#endif
                .CreateLogger();

            Log.Information("Stratum Desktop starting");
        }

        private static void EnsureDataDirectory()
        {
            var dataDir = GetDataDirectory();
            Directory.CreateDirectory(dataDir);
            Directory.CreateDirectory(Path.Combine(dataDir, "logs"));
        }

        public static string GetDataDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Stratum");
        }
    }
}
