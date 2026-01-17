// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Autofac;
using Serilog;
using Stratum.Core.Backup;
using Stratum.Core.Backup.Encryption;
using Stratum.Core.Service;
using Stratum.Desktop.Views;

namespace Stratum.Desktop.Panels
{
    public partial class BackupPanel : UserControl
    {
        private readonly ILogger _log = Log.ForContext<BackupPanel>();
        private readonly IBackupService _backupService;
        private readonly IRestoreService _restoreService;

        public BackupPanel()
        {
            InitializeComponent();
            _backupService = App.Container.Resolve<IBackupService>();
            _restoreService = App.Container.Resolve<IRestoreService>();
        }

        private async void CreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Stratum Backup (*.stratum)|*.stratum|HTML Export (*.html)|*.html|URI List (*.txt)|*.txt",
                DefaultExt = ".stratum",
                FileName = $"stratum_backup_{DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                bool success;

                switch (extension)
                {
                    case ".stratum":
                        success = await CreateEncryptedBackupAsync(dialog.FileName);
                        break;

                    case ".html":
                        success = await CreateHtmlBackupAsync(dialog.FileName);
                        break;

                    case ".txt":
                        success = await CreateUriListBackupAsync(dialog.FileName);
                        break;

                    default:
                        success = await CreateEncryptedBackupAsync(dialog.FileName);
                        break;
                }

                if (success)
                {
                    MessageBox.Show($"Backup saved to {dialog.FileName}", "Backup Created", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to create backup");
                MessageBox.Show($"Failed to create backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> CreateEncryptedBackupAsync(string path)
        {
            var passwordDialog = new PasswordDialog("Enter a password to encrypt the backup:", true)
            {
                Owner = Window.GetWindow(this)
            };
            if (passwordDialog.ShowDialog() != true) return false;

            var backup = await _backupService.CreateBackupAsync();
            var encryption = new StrongBackupEncryption();
            var data = await encryption.EncryptAsync(backup, passwordDialog.Password);
            await File.WriteAllBytesAsync(path, data);
            _log.Information("Created encrypted backup at {Path}", path);
            return true;
        }

        private async Task<bool> CreateHtmlBackupAsync(string path)
        {
            var result = MessageBox.Show(
                "HTML backup will contain unencrypted secret keys.\nContinue?",
                "Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return false;

            var htmlBackup = await _backupService.CreateHtmlBackupAsync();
            await File.WriteAllTextAsync(path, htmlBackup.ToString());
            _log.Information("Created HTML backup at {Path}", path);
            return true;
        }

        private async Task<bool> CreateUriListBackupAsync(string path)
        {
            var result = MessageBox.Show(
                "URI list backup will contain unencrypted secret keys.\nContinue?",
                "Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return false;

            var uriBackup = await _backupService.CreateUriListBackupAsync();
            await File.WriteAllTextAsync(path, uriBackup.ToString());
            _log.Information("Created URI list backup at {Path}", path);
            return true;
        }

        private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Stratum Backup (*.stratum)|*.stratum|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var data = await File.ReadAllBytesAsync(dialog.FileName);
                var encryption = new StrongBackupEncryption();

                if (!encryption.CanBeDecrypted(data))
                {
                    MessageBox.Show("This file does not appear to be a valid Stratum backup.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var passwordDialog = new PasswordDialog("Enter the backup password:", false)
                {
                    Owner = Window.GetWindow(this)
                };
                if (passwordDialog.ShowDialog() != true) return;

                Backup backup;
                try
                {
                    backup = await encryption.DecryptAsync(data, passwordDialog.Password);
                }
                catch (BackupPasswordException)
                {
                    MessageBox.Show("Invalid password.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var confirmResult = MessageBox.Show(
                    "Do you want to add new authenticators or replace all existing data?\n\nYes = Add (keep existing)\nNo = Replace (delete existing)",
                    "Restore Mode",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Cancel) return;

                RestoreResult result;
                if (confirmResult == MessageBoxResult.Yes)
                {
                    result = await _restoreService.RestoreAndUpdateAsync(backup);
                }
                else
                {
                    result = await _restoreService.RestoreAsync(backup);
                }

                MessageBox.Show(
                    $"Restored {result.AddedAuthenticatorCount} authenticators, {result.AddedCategoryCount} categories.\nPlease restart the application.",
                    "Restore Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _log.Information("Restored backup: {Added} authenticators, {Categories} categories",
                    result.AddedAuthenticatorCount, result.AddedCategoryCount);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to restore backup");
                MessageBox.Show($"Failed to restore backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
