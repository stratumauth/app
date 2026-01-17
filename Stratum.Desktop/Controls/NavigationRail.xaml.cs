// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows;
using System.Windows.Controls;

namespace Stratum.Desktop.Controls
{
    public partial class NavigationRail : UserControl
    {
        public event EventHandler<string> NavigationChanged;

        public NavigationRail()
        {
            InitializeComponent();
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button && button.Tag is string tag)
            {
                NavigationChanged?.Invoke(this, tag);
            }
        }

        public void SelectItem(string tag)
        {
            switch (tag)
            {
                case "Home":
                    HomeButton.IsChecked = true;
                    break;
                case "Settings":
                    SettingsButton.IsChecked = true;
                    break;
                case "Categories":
                    CategoriesButton.IsChecked = true;
                    break;
                case "Import":
                    ImportButton.IsChecked = true;
                    break;
                case "Backup":
                    BackupButton.IsChecked = true;
                    break;
                case "About":
                    AboutButton.IsChecked = true;
                    break;
            }
        }
    }
}
