// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stratum.Desktop.ViewModels;

namespace Stratum.Desktop.Panels
{
    public partial class HomePanel : UserControl
    {
        public HomePanel()
        {
            InitializeComponent();
        }

        public void FocusSearchBox()
        {
            SearchBox?.Focus();
            SearchBox?.SelectAll();
        }

        private void AuthenticatorCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && sender is Border border)
            {
                if (border.DataContext is AuthenticatorViewModel auth)
                {
                    viewModel.CopyCodeCommand.Execute(auth);
                }
            }
        }
    }
}
