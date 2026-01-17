// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows;
using System.Windows.Controls;
using Autofac;
using Stratum.Desktop.Panels;
using Stratum.Desktop.ViewModels;

namespace Stratum.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private HomePanel _homePanel;

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

            // Load Home panel by default
            NavigateToHome();
        }

        private void NavigationRail_NavigationChanged(object sender, string tag)
        {
            switch (tag)
            {
                case "Home":
                    NavigateToHome();
                    break;
                case "Settings":
                    NavigateToSettings();
                    break;
                case "Categories":
                    NavigateToCategories();
                    break;
                case "Import":
                    NavigateToImport();
                    break;
                case "Backup":
                    NavigateToBackup();
                    break;
                case "About":
                    NavigateToAbout();
                    break;
            }
        }

        private void NavigateToHome()
        {
            if (_homePanel == null)
            {
                _homePanel = new HomePanel { DataContext = _viewModel };
            }
            ContentFrame.Navigate(_homePanel);
        }

        private void NavigateToSettings()
        {
            var settingsPanel = new SettingsPanel();
            ContentFrame.Navigate(settingsPanel);
        }

        private void NavigateToCategories()
        {
            var categoriesPanel = new CategoriesPanel();
            ContentFrame.Navigate(categoriesPanel);
        }

        private void NavigateToImport()
        {
            // Open import dialog for now (will be converted to panel later)
            var importDialog = new Views.ImportDialog
            {
                Owner = this
            };
            importDialog.ShowDialog();

            // Return to Home
            NavigationRail.SelectItem("Home");
        }

        private void NavigateToBackup()
        {
            var backupPanel = new BackupPanel();
            ContentFrame.Navigate(backupPanel);
        }

        private void NavigateToAbout()
        {
            var aboutPanel = new AboutPanel();
            ContentFrame.Navigate(aboutPanel);
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
