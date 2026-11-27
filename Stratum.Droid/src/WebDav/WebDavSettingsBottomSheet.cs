// Copyright (C) 2026 muort
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Work;
using Google.Android.Material.Button;
using Google.Android.Material.MaterialSwitch;
using Google.Android.Material.TextField;
using Google.Android.Material.TextView;
using Stratum.Droid.Interface.Fragment;

namespace Stratum.Droid.WebDav
{
    /// <summary>
    /// 设置页里的「云备份（WebDAV）」表单。整个界面都是新增文件，不改上游布局。
    /// </summary>
    public class WebDavSettingsBottomSheet : BottomSheet
    {
        private WebDavConfig _config;

        private MaterialSwitch _switchEnabled;
        private MaterialSwitch _switchTrustAll;
        private TextInputEditText _editUrl;
        private TextInputEditText _editDirectory;
        private TextInputEditText _editUsername;
        private TextInputEditText _editPassword;
        private MaterialTextView _textStatus;
        private MaterialButton _buttonTest;
        private MaterialButton _buttonBackupNow;

        public WebDavSettingsBottomSheet() : base(Resource.Layout.sheetWebDavSetup,
            Resource.String.webdavSettingsTitle)
        {
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            _config = new WebDavConfig(RequireContext());

            _switchEnabled = view.FindViewById<MaterialSwitch>(Resource.Id.switchEnabled);
            _switchTrustAll = view.FindViewById<MaterialSwitch>(Resource.Id.switchTrustAll);
            _editUrl = view.FindViewById<TextInputEditText>(Resource.Id.editUrl);
            _editDirectory = view.FindViewById<TextInputEditText>(Resource.Id.editDirectory);
            _editUsername = view.FindViewById<TextInputEditText>(Resource.Id.editUsername);
            _editPassword = view.FindViewById<TextInputEditText>(Resource.Id.editPassword);
            _textStatus = view.FindViewById<MaterialTextView>(Resource.Id.textWebDavStatus);
            _buttonTest = view.FindViewById<MaterialButton>(Resource.Id.buttonTest);
            _buttonBackupNow = view.FindViewById<MaterialButton>(Resource.Id.buttonBackupNow);

            _switchEnabled.Checked = _config.Enabled;
            _switchTrustAll.Checked = _config.TrustAllCertificates;
            _editUrl.Text = _config.Url;
            _editDirectory.Text = _config.Directory;
            _editUsername.Text = _config.Username;
            _editPassword.Text = _config.Password;
            _textStatus.Text = _config.LastStatus ?? "";

            view.FindViewById<MaterialButton>(Resource.Id.buttonSave).Click += delegate
            {
                Save();
                Toast.MakeText(RequireContext(), Resource.String.webdavSaved, ToastLength.Short).Show();
                Dismiss();
            };

            _buttonTest.Click += async delegate { await TestConnectionAsync(); };
            _buttonBackupNow.Click += delegate { BackupNow(); };

            return view;
        }

        private void Save()
        {
            _config.Enabled = _switchEnabled.Checked;
            _config.Url = _editUrl.Text;
            _config.Directory = _editDirectory.Text;
            _config.Username = _editUsername.Text;
            _config.Password = _editPassword.Text;
            _config.TrustAllCertificates = _switchTrustAll.Checked;
            WebDavSettingsEntry.RefreshSummary(RequireContext());
        }

        private async Task TestConnectionAsync()
        {
            Save();

            if (!_config.IsConfigured)
            {
                Toast.MakeText(RequireContext(), Resource.String.webdavNotConfigured, ToastLength.Short).Show();
                return;
            }

            _buttonTest.Enabled = false;
            _textStatus.SetText(Resource.String.webdavTesting);

            try
            {
                await WebDavBackupService.TestAsync(RequireContext());
                _config.LastStatus = GetString(Resource.String.webdavTestOk);
                _textStatus.SetText(Resource.String.webdavTestOk);
                Toast.MakeText(RequireContext(), Resource.String.webdavTestOk, ToastLength.Short).Show();
            }
            catch (Exception e)
            {
                _config.LastStatus = GetString(Resource.String.webdavTestFailed, e.Message);
                _textStatus.Text = _config.LastStatus;
                Toast.MakeText(RequireContext(), _config.LastStatus, ToastLength.Long).Show();
            }
            finally
            {
                _buttonTest.Enabled = true;
            }
        }

        private void BackupNow()
        {
            Save();

            if (!_config.IsConfigured)
            {
                Toast.MakeText(RequireContext(), Resource.String.webdavNotConfigured, ToastLength.Short).Show();
                return;
            }

            var context = RequireContext();
            var preferences = new PreferenceWrapper(context);

            if (preferences.AutoBackupUri == null)
            {
                Toast.MakeText(context, Resource.String.webdavNeedAutoBackup, ToastLength.Short).Show();
                return;
            }

            preferences.AutoBackupTrigger = true;
            Toast.MakeText(context, Resource.String.webdavBackupScheduled, ToastLength.Short).Show();

            var request = new OneTimeWorkRequest.Builder(typeof(AutoBackupWorker)).Build();
            WorkManager.GetInstance(context)
                .EnqueueUniqueWork(AutoBackupWorker.Name, ExistingWorkPolicy.Replace!, request);
        }
    }
}
