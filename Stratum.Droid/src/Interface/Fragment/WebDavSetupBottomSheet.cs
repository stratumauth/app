// Copyright (C) 2024 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.Work;
using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.Dialog;
using Google.Android.Material.MaterialSwitch;
using Google.Android.Material.TextField;
using Google.Android.Material.TextView;
using Java.Util.Concurrent;
using Serilog;
using Stratum.Core.WebDav;
using Stratum.Droid.Activity;

namespace Stratum.Droid.Interface.Fragment
{
    public class WebDavSetupBottomSheet : BottomSheet
    {
        private readonly ILogger _log = Log.ForContext<WebDavSetupBottomSheet>();

        private PreferenceWrapper _preferences;
        private SecureStorageWrapper _secureStorageWrapper;

        private MaterialTextView _serverUrlStatusText;
        private MaterialTextView _credentialsStatusText;
        private MaterialTextView _remotePathStatusText;

        private MaterialSwitch _webDavEnabledSwitch;
        private MaterialSwitch _webDavRestoreEnabledSwitch;
        private MaterialButton _testConnectionButton;
        private MaterialButton _okButton;

        public WebDavSetupBottomSheet() : base(Resource.Layout.sheetWebDavSetup,
            Resource.String.webDavBackupTitle)
        {
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _preferences = new PreferenceWrapper(RequireContext());
            _secureStorageWrapper = new SecureStorageWrapper(RequireContext());
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            var serverUrlCard = view.FindViewById<MaterialCardView>(Resource.Id.cardServerUrl);
            serverUrlCard.Click += OnServerUrlClick;

            var credentialsCard = view.FindViewById<MaterialCardView>(Resource.Id.cardCredentials);
            credentialsCard.Click += OnCredentialsClick;

            var remotePathCard = view.FindViewById<MaterialCardView>(Resource.Id.cardRemotePath);
            remotePathCard.Click += OnRemotePathClick;

            _serverUrlStatusText = view.FindViewById<MaterialTextView>(Resource.Id.textServerUrlStatus);
            _credentialsStatusText = view.FindViewById<MaterialTextView>(Resource.Id.textCredentialsStatus);
            _remotePathStatusText = view.FindViewById<MaterialTextView>(Resource.Id.textRemotePathStatus);

            _webDavEnabledSwitch = view.FindViewById<MaterialSwitch>(Resource.Id.switchWebDavEnabled);
            _webDavRestoreEnabledSwitch = view.FindViewById<MaterialSwitch>(Resource.Id.switchWebDavRestoreEnabled);

            _testConnectionButton = view.FindViewById<MaterialButton>(Resource.Id.buttonTestConnection);
            _testConnectionButton.Click += OnTestConnectionClick;

            _okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOk);
            _okButton.Click += delegate { Dismiss(); };

            UpdateServerUrlStatus();
            UpdateCredentialsStatus();
            UpdateRemotePathStatus();
            UpdateSwitchStates();

            return view;
        }

        public override void OnDismiss(Android.Content.IDialogInterface dialog)
        {
            base.OnDismiss(dialog);

            _preferences.WebDavBackupEnabled = _webDavEnabledSwitch.Checked;
            _preferences.WebDavRestoreEnabled = _webDavRestoreEnabledSwitch.Checked;

            UpdateAutoBackupWorker();
        }

        private void UpdateAutoBackupWorker()
        {
            var workManager = WorkManager.GetInstance(RequireContext());

            var localEnabled = _preferences.AutoBackupEnabled;
            var webDavEnabled = _preferences.WebDavBackupEnabled || _preferences.WebDavRestoreEnabled;

            if (localEnabled || webDavEnabled)
            {
                var constraintsBuilder = new Constraints.Builder();

                if (webDavEnabled)
                {
                    constraintsBuilder.SetRequiredNetworkType(AndroidX.Work.NetworkType.Connected!);
                }

                var workRequest =
                    new PeriodicWorkRequest.Builder(typeof(AutoBackupWorker), 15, TimeUnit.Minutes!)
                        .SetConstraints(constraintsBuilder.Build())
                        .Build();

                workManager.EnqueueUniquePeriodicWork(AutoBackupWorker.Name,
                    ExistingPeriodicWorkPolicy.Replace!, workRequest);
            }
            else
            {
                workManager.CancelUniqueWork(AutoBackupWorker.Name);
            }
        }

        private void OnServerUrlClick(object sender, EventArgs e)
        {
            var editText = new TextInputEditText(RequireContext());
            editText.SetHint(Resource.String.webDavServerUrlHint);
            editText.InputType = InputTypes.ClassText | InputTypes.TextVariationUri;
            editText.SetSingleLine(true);

            var currentUrl = _preferences.WebDavUrl;
            if (!string.IsNullOrEmpty(currentUrl))
            {
                editText.Text = currentUrl;
            }

            var container = CreateDialogInputContainer(editText);

            new MaterialAlertDialogBuilder(RequireContext())
                .SetTitle(Resource.String.webDavServerUrl)
                .SetView(container)
                .SetNegativeButton(Resource.String.cancel, delegate { })
                .SetPositiveButton(Resource.String.ok, delegate
                {
                    var url = editText.Text?.Trim();

                    if (string.IsNullOrEmpty(url))
                    {
                        _preferences.WebDavUrl = null;
                    }
                    else
                    {
                        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            url = "https://" + url;
                        }

                        _preferences.WebDavUrl = url;
                    }

                    UpdateServerUrlStatus();
                    UpdateSwitchStates();
                })
                .Create()
                .Show();
        }

        private void OnCredentialsClick(object sender, EventArgs e)
        {
            var usernameEdit = new TextInputEditText(RequireContext());
            usernameEdit.SetHint(Resource.String.webDavUsername);
            usernameEdit.InputType = InputTypes.ClassText | InputTypes.TextVariationNormal;
            usernameEdit.SetSingleLine(true);

            var currentUsername = _secureStorageWrapper.GetWebDavUsername();
            if (!string.IsNullOrEmpty(currentUsername))
            {
                usernameEdit.Text = currentUsername;
            }

            var passwordEdit = new TextInputEditText(RequireContext());
            passwordEdit.SetHint(Resource.String.webDavPassword);
            passwordEdit.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
            passwordEdit.SetSingleLine(true);

            var currentPassword = _secureStorageWrapper.GetWebDavPassword();
            if (!string.IsNullOrEmpty(currentPassword))
            {
                passwordEdit.Text = currentPassword;
            }

            var container = new LinearLayout(RequireContext())
            {
                Orientation = Orientation.Vertical
            };

            var padding = (int) (20 * Resources.DisplayMetrics.Density);
            container.SetPadding(padding, padding, padding, 0);
            container.AddView(usernameEdit);
            container.AddView(passwordEdit);

            new MaterialAlertDialogBuilder(RequireContext())
                .SetTitle(Resource.String.webDavCredentials)
                .SetView(container)
                .SetNegativeButton(Resource.String.cancel, delegate { })
                .SetPositiveButton(Resource.String.ok, delegate
                {
                    _secureStorageWrapper.SetWebDavUsername(usernameEdit.Text?.Trim());
                    _secureStorageWrapper.SetWebDavPassword(passwordEdit.Text);
                    UpdateCredentialsStatus();
                    UpdateSwitchStates();
                })
                .Create()
                .Show();
        }

        private void OnRemotePathClick(object sender, EventArgs e)
        {
            var editText = new TextInputEditText(RequireContext());
            editText.SetHint(Resource.String.webDavRemotePathHint);
            editText.InputType = InputTypes.ClassText;
            editText.SetSingleLine(true);
            editText.Text = _preferences.WebDavRemotePath;

            var container = CreateDialogInputContainer(editText);

            new MaterialAlertDialogBuilder(RequireContext())
                .SetTitle(Resource.String.webDavRemotePath)
                .SetView(container)
                .SetNegativeButton(Resource.String.cancel, delegate { })
                .SetPositiveButton(Resource.String.ok, delegate
                {
                    var path = editText.Text?.Trim();

                    if (string.IsNullOrEmpty(path))
                    {
                        path = "/stratum-backups/";
                    }

                    if (!path.StartsWith('/'))
                    {
                        path = "/" + path;
                    }

                    if (!path.EndsWith('/'))
                    {
                        path += "/";
                    }

                    _preferences.WebDavRemotePath = path;
                    UpdateRemotePathStatus();
                })
                .Create()
                .Show();
        }

        private async void OnTestConnectionClick(object sender, EventArgs e)
        {
            var url = _preferences.WebDavUrl;
            var username = _secureStorageWrapper.GetWebDavUsername();
            var password = _secureStorageWrapper.GetWebDavPassword();

            if (string.IsNullOrEmpty(url))
            {
                Toast.MakeText(Context, Resource.String.webDavUrlRequired, ToastLength.Short).Show();
                return;
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Toast.MakeText(Context, Resource.String.webDavCredentialsRequired, ToastLength.Short).Show();
                return;
            }

            _testConnectionButton.Enabled = false;

            try
            {
                using var client = new WebDavClient(url, username, password);
                var success = await client.TestConnectionAsync();

                if (success)
                {
                    Toast.MakeText(Context, Resource.String.webDavTestSuccess, ToastLength.Short).Show();
                }
                else
                {
                    var message = string.Format(GetString(Resource.String.webDavTestFailure), "Server returned error");
                    Toast.MakeText(Context, message, ToastLength.Long).Show();
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "WebDAV connection test failed");
                var message = string.Format(GetString(Resource.String.webDavTestFailure), ex.Message);
                Toast.MakeText(Context, message, ToastLength.Long).Show();
            }
            finally
            {
                _testConnectionButton.Enabled = true;
            }
        }

        private FrameLayout CreateDialogInputContainer(View input)
        {
            var container = new FrameLayout(RequireContext());
            var padding = (int) (20 * Resources.DisplayMetrics.Density);
            container.SetPadding(padding, padding, padding, 0);
            container.AddView(input);
            return container;
        }

        private void UpdateServerUrlStatus()
        {
            var url = _preferences.WebDavUrl;

            if (string.IsNullOrEmpty(url))
            {
                _serverUrlStatusText.SetText(Resource.String.webDavNotConfigured);
                return;
            }

            _serverUrlStatusText.Text = string.Format(GetString(Resource.String.webDavServerUrlSetTo), url);
        }

        private void UpdateCredentialsStatus()
        {
            var username = _secureStorageWrapper.GetWebDavUsername();

            _credentialsStatusText.SetText(string.IsNullOrEmpty(username)
                ? Resource.String.webDavCredentialsNotSet
                : Resource.String.webDavCredentialsSet);
        }

        private void UpdateRemotePathStatus()
        {
            var path = _preferences.WebDavRemotePath;
            _remotePathStatusText.Text = string.Format(GetString(Resource.String.webDavRemotePathSetTo), path);
        }

        private void UpdateSwitchStates()
        {
            var canBeEnabled = !string.IsNullOrEmpty(_preferences.WebDavUrl) &&
                               !string.IsNullOrEmpty(_secureStorageWrapper.GetWebDavUsername()) &&
                               !string.IsNullOrEmpty(_secureStorageWrapper.GetWebDavPassword());

            _webDavEnabledSwitch.Enabled = canBeEnabled;
            _webDavRestoreEnabledSwitch.Enabled = canBeEnabled;
            _testConnectionButton.Enabled = canBeEnabled;

            _webDavEnabledSwitch.Checked = canBeEnabled && _preferences.WebDavBackupEnabled;
            _webDavRestoreEnabledSwitch.Checked = canBeEnabled && _preferences.WebDavRestoreEnabled;

            if (!canBeEnabled)
            {
                _webDavEnabledSwitch.Checked = false;
                _webDavRestoreEnabledSwitch.Checked = false;
            }
        }
    }
}
