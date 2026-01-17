// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Windows;
using Autofac;
using Microsoft.Win32;
using Serilog;
using Stratum.Core;
using Stratum.Core.Backup;
using Stratum.Core.Converter;
using Stratum.Core.Service;

namespace Stratum.Desktop.Views
{
    public partial class ImportDialog : Window
    {
        private readonly ILogger _log = Log.ForContext<ImportDialog>();
        private readonly IImportService _importService;
        private readonly IIconResolver _iconResolver;
        private readonly ICustomIconDecoder _customIconDecoder;

        private static readonly (string Name, string Filter, Func<IIconResolver, ICustomIconDecoder, BackupConverter> Factory)[] Sources =
        {
            ("Google Authenticator", "All Files (*.*)|*.*", (ir, icd) => new GoogleAuthenticatorBackupConverter(ir)),
            ("Aegis Authenticator", "JSON Files (*.json)|*.json|All Files (*.*)|*.*", (ir, icd) => new AegisBackupConverter(ir, icd)),
            ("2FAS Authenticator", "JSON Files (*.json)|*.json|2FAS Files (*.2fas)|*.2fas", (ir, icd) => new TwoFasBackupConverter(ir)),
            ("Bitwarden", "JSON Files (*.json)|*.json", (ir, icd) => new BitwardenBackupConverter(ir)),
            ("andOTP", "JSON Files (*.json)|*.json", (ir, icd) => new AndOtpBackupConverter(ir)),
            ("FreeOTP", "JSON/XML Files (*.json;*.xml)|*.json;*.xml", (ir, icd) => new FreeOtpBackupConverter(ir)),
            ("FreeOTP+", "JSON Files (*.json)|*.json", (ir, icd) => new FreeOtpPlusBackupConverter(ir)),
            ("Authenticator Plus", "SQLite Files (*.db)|*.db|All Files (*.*)|*.*", (ir, icd) => new AuthenticatorPlusBackupConverter(ir)),
            ("WinAuth", "Text Files (*.txt)|*.txt|All Files (*.*)|*.*", (ir, icd) => new WinAuthBackupConverter(ir)),
            ("TOTP Authenticator", "JSON Files (*.json)|*.json", (ir, icd) => new TotpAuthenticatorBackupConverter(ir)),
            ("Ente Auth", "Text/JSON Files (*.txt;*.json)|*.txt;*.json", (ir, icd) => new EnteAuthBackupConverter(ir)),
            ("LastPass Authenticator", "JSON Files (*.json)|*.json", (ir, icd) => new LastPassBackupConverter(ir)),
            ("Proton Pass", "JSON Files (*.json)|*.json", (ir, icd) => new ProtonAuthenticatorBackupConverter(ir)),
            ("KeePass", "KeePass Files (*.kdbx)|*.kdbx", (ir, icd) => new KeePassBackupConverter(ir)),
            ("URI List", "Text Files (*.txt)|*.txt", (ir, icd) => new UriListBackupConverter(ir)),
        };

        public ImportDialog()
        {
            InitializeComponent();
            _importService = App.Container.Resolve<IImportService>();
            _iconResolver = App.Container.Resolve<IIconResolver>();
            _customIconDecoder = App.Container.Resolve<ICustomIconDecoder>();
            SourceListBox.SelectedIndex = 0;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = SourceListBox.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= Sources.Length)
            {
                MessageBox.Show("Please select an import source.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var source = Sources[selectedIndex];

            var dialog = new OpenFileDialog
            {
                Filter = source.Filter,
                Title = $"Select {source.Name} backup file"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var data = await File.ReadAllBytesAsync(dialog.FileName);
                var converter = source.Factory(_iconResolver, _customIconDecoder);

                string password = null;

                // Check if the converter needs a password
                if (converter.PasswordPolicy == BackupConverter.BackupPasswordPolicy.Always)
                {
                    var passwordDialog = new PasswordDialog($"Enter the {source.Name} backup password:", false);
                    if (passwordDialog.ShowDialog() != true) return;
                    password = passwordDialog.Password;
                }
                else if (converter.PasswordPolicy == BackupConverter.BackupPasswordPolicy.Maybe)
                {
                    var askPassword = MessageBox.Show(
                        $"Does this {source.Name} backup have a password?",
                        "Password",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (askPassword == MessageBoxResult.Yes)
                    {
                        var passwordDialog = new PasswordDialog($"Enter the {source.Name} backup password:", false);
                        if (passwordDialog.ShowDialog() != true) return;
                        password = passwordDialog.Password;
                    }
                }

                var (conversionResult, restoreResult) = await _importService.ImportAsync(converter, data, password);

                if (conversionResult.Failures.Any())
                {
                    var failureCount = conversionResult.Failures.Count();
                    var successCount = conversionResult.Backup.Authenticators.Count();
                    MessageBox.Show(
                        $"Parsed {successCount} authenticators with {failureCount} failures.",
                        "Partial Import",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                MessageBox.Show(
                    $"Imported {restoreResult.AddedAuthenticatorCount} authenticators.\nPlease restart the application.",
                    "Import Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _log.Information("Imported {Count} authenticators from {Source}",
                    restoreResult.AddedAuthenticatorCount, source.Name);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to import from {Source}", source.Name);
                MessageBox.Show($"Failed to import: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
