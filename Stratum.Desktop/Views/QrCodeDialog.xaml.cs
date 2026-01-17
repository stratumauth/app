// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;
using Stratum.Core.Entity;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.Views
{
    public partial class QrCodeDialog : Window
    {
        private readonly string _uri;

        public QrCodeDialog(Authenticator authenticator)
        {
            InitializeComponent();

            IssuerText.Text = authenticator.Issuer;
            UsernameText.Text = authenticator.Username ?? "";
            UsernameText.Visibility = string.IsNullOrEmpty(authenticator.Username)
                ? Visibility.Collapsed
                : Visibility.Visible;

            _uri = authenticator.GetUri();
            GenerateQrCode();
        }

        private void GenerateQrCode()
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(_uri, QRCodeGenerator.ECCLevel.M);
                using var qrCode = new BitmapByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(10);

                var bitmap = new BitmapImage();
                using (var ms = new MemoryStream(qrCodeBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }

                QrCodeImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationManager.GetString("QrCodeGenerateFailed"), ex.Message), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyUriButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_uri);
                MessageBox.Show(LocalizationManager.GetString("UriCopied"), LocalizationManager.GetString("Information"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationManager.GetString("CopyFailed"), ex.Message), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
