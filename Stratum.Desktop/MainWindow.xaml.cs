// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System.Windows;
using System.Windows.Input;
using Autofac;
using Stratum.Desktop.ViewModels;

namespace Stratum.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = App.Container.Resolve<MainViewModel>();
            DataContext = _viewModel;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }

        private void AuthenticatorCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is AuthenticatorViewModel auth)
            {
                _viewModel.CopyCodeCommand.Execute(auth);
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
