// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System.Windows;

namespace Stratum.Desktop.Views
{
    public partial class PasswordDialog : Window
    {
        private readonly bool _requireConfirmation;

        public string Password { get; private set; }

        public PasswordDialog(string prompt, bool requireConfirmation)
        {
            InitializeComponent();
            PromptText.Text = prompt;
            _requireConfirmation = requireConfirmation;

            if (requireConfirmation)
            {
                ConfirmPanel.Visibility = Visibility.Visible;
            }

            PasswordBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Password cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            if (_requireConfirmation && password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Passwords do not match.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                ConfirmPasswordBox.Focus();
                return;
            }

            Password = password;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
