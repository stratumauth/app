// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows;
using System.Windows.Controls;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Generator;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.Views
{
    public partial class AddAuthenticatorDialog : Window
    {
        private Authenticator _originalAuthenticator;

        public Authenticator Result { get; private set; }
        public bool IsEditMode { get; private set; }

        public AddAuthenticatorDialog()
        {
            InitializeComponent();
        }

        public void LoadAuthenticator(Authenticator authenticator)
        {
            if (authenticator == null) return;

            IsEditMode = true;
            _originalAuthenticator = authenticator;

            Title = LocalizationManager.GetString("EditAuthenticatorTitle");
            SaveButton.Content = LocalizationManager.GetString("Save");

            IssuerTextBox.Text = authenticator.Issuer ?? string.Empty;
            UsernameTextBox.Text = authenticator.Username ?? string.Empty;
            SecretTextBox.Text = authenticator.Secret ?? string.Empty;

            TypeComboBox.SelectedIndex = authenticator.Type switch
            {
                AuthenticatorType.Totp => 0,
                AuthenticatorType.Hotp => 1,
                AuthenticatorType.SteamOtp => 2,
                AuthenticatorType.MobileOtp => 3,
                AuthenticatorType.YandexOtp => 4,
                _ => 0
            };

            AlgorithmComboBox.SelectedIndex = authenticator.Algorithm switch
            {
                HashAlgorithm.Sha1 => 0,
                HashAlgorithm.Sha256 => 1,
                HashAlgorithm.Sha512 => 2,
                _ => 0
            };

            DigitsComboBox.SelectedIndex = authenticator.Digits switch
            {
                6 => 0,
                7 => 1,
                8 => 2,
                _ => 0
            };

            PeriodTextBox.Text = authenticator.Period.ToString();
            CounterTextBox.Text = authenticator.Counter.ToString();
            PinPasswordBox.Password = authenticator.Pin ?? string.Empty;

            UpdatePanels(TypeComboBox.SelectedIndex);
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            UpdatePanels(TypeComboBox.SelectedIndex);
        }

        private void UpdatePanels(int selectedIndex)
        {
            PinPanel.Visibility = (selectedIndex == 3 || selectedIndex == 4)
                ? Visibility.Visible
                : Visibility.Collapsed;

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
                var validationTitle = LocalizationManager.GetString("Validation");

                if (string.IsNullOrEmpty(issuer))
                {
                    MessageBox.Show(LocalizationManager.GetString("ValidationIssuer"), validationTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                    IssuerTextBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(secret))
                {
                    MessageBox.Show(LocalizationManager.GetString("ValidationSecret"), validationTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
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

                if (_originalAuthenticator != null)
                {
                    auth.Icon = _originalAuthenticator.Icon;
                    auth.CopyCount = _originalAuthenticator.CopyCount;
                    auth.Ranking = _originalAuthenticator.Ranking;
                }

                if (type == AuthenticatorType.MobileOtp || type == AuthenticatorType.YandexOtp)
                {
                    var pin = PinPasswordBox.Password;
                    if (string.IsNullOrEmpty(pin))
                    {
                        MessageBox.Show(LocalizationManager.GetString("ValidationPIN"), validationTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                        PinPasswordBox.Focus();
                        return;
                    }
                    auth.Pin = pin;
                }

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

                auth.Validate();

                Result = auth;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationManager.GetString("InvalidAuthenticator"), ex.Message), LocalizationManager.GetString("Validation"), MessageBoxButton.OK, MessageBoxImage.Error);
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
