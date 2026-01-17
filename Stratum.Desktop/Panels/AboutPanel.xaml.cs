// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Stratum.Desktop.Panels
{
    public partial class AboutPanel : UserControl
    {
        public AboutPanel()
        {
            InitializeComponent();
            Loaded += AboutPanel_Loaded;
        }

        private void AboutPanel_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLocalizedText();
            UpdateVersionInfo();
        }

        private void UpdateLocalizedText()
        {
            // Update localized text from resource dictionary
            if (Application.Current.Resources["Version"] is string version)
                VersionLabel.Text = version;

            if (Application.Current.Resources["GitHubRepository"] is string gitHubRepo)
                GitHubRepositoryRun.Text = gitHubRepo;

            if (Application.Current.Resources["ReportIssue"] is string reportIssue)
                ReportIssueRun.Text = reportIssue;
        }

        private void UpdateVersionInfo()
        {
            // Get version from assembly
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

            // Update version display
            VersionNumber.Text = versionString;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
