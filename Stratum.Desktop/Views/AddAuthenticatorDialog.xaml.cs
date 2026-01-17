// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows;
using System.Windows.Controls;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Generator;

namespace Stratum.Desktop.Views
{
    public partial class AddAuthenticatorDialog : Window
    {
        public Authenticator Result { get; private set; }

        public AddAuthenticatorDialog()
        {
            InitializeComponent();
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var selectedIndex = TypeComboBox.SelectedIndex;

            // Show/hide PIN panel for mOTP and Yandex
            PinPanel.Visibility = (selectedIndex == 3 || selectedIndex == 4)
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Show/hide advanced options based on type
            AlgorithmPanel.Visibility = (selectedIndex <= 1)
                ? Visibility.Visible
                : Visibility.Collapsed;

            DigitsPanel.Visibility = (selectedIndex <= 1)
                ? Visibility.Visible
                : Visibility.Collapsed;

            PeriodPanel.Visibility = (selectedIndex == 0)
                ? Visibility.Visible
                : Visibility.Collapsed;

            CounterPanel.Visibility = (selectedIndex == 1)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var issuer = IssuerTextBox.Text.Trim();
                var username = UsernameTextBox.Text.Trim();
                var secret = SecretTextBox.Text.Trim().ToUpperInvariant().Replace(" ", "");

                if (string.IsNullOrEmpty(issuer))
                {
                    MessageBox.Show("Issuer is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IssuerTextBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(secret))
                {
                    MessageBox.Show("Secret is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SecretTextBox.Focus();
                    return;
                }

                var type = GetSelectedType();
                var auth = new Authenticator
                {
                    Type = type,
                    Issuer = issuer,
                    Username = username,
                    Secret = secret
                };

                // Set PIN for mOTP and Yandex
                if (type == AuthenticatorType.MobileOtp || type == AuthenticatorType.YandexOtp)
                {
                    var pin = PinPasswordBox.Password;
                    if (string.IsNullOrEmpty(pin))
                    {
                        MessageBox.Show("PIN is required for this type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        PinPasswordBox.Focus();
                        return;
                    }
                    auth.Pin = pin;
                }

                // Set algorithm
                if (type == AuthenticatorType.Totp || type == AuthenticatorType.Hotp)
                {
                    auth.Algorithm = AlgorithmComboBox.SelectedIndex switch
                    {
                        0 => HashAlgorithm.Sha1,
                        1 => HashAlgorithm.Sha256,
                        2 => HashAlgorithm.Sha512,
                        _ => HashAlgorithm.Sha1
                    };

                    auth.Digits = DigitsComboBox.SelectedIndex switch
                    {
                        0 => 6,
                        1 => 7,
                        2 => 8,
                        _ => 6
                    };
                }

                // Set period for TOTP
                if (type == AuthenticatorType.Totp)
                {
                    if (int.TryParse(PeriodTextBox.Text, out var period) && period > 0)
                    {
                        auth.Period = period;
                    }
                    else
                    {
                        auth.Period = 30;
                    }
                }

                // Set counter for HOTP
                if (type == AuthenticatorType.Hotp)
                {
                    if (long.TryParse(CounterTextBox.Text, out var counter) && counter >= 0)
                    {
                        auth.Counter = counter;
                    }
                    else
                    {
                        auth.Counter = 0;
                    }
                }

                // Validate the authenticator
                auth.Validate();

                Result = auth;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid authenticator: {ex.Message}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private AuthenticatorType GetSelectedType()
        {
            return TypeComboBox.SelectedIndex switch
            {
                0 => AuthenticatorType.Totp,
                1 => AuthenticatorType.Hotp,
                2 => AuthenticatorType.SteamOtp,
                3 => AuthenticatorType.MobileOtp,
                4 => AuthenticatorType.YandexOtp,
                _ => AuthenticatorType.Totp
            };
        }
    }
}
