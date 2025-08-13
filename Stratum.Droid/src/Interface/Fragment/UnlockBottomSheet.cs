// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Button;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.TextField;
using Serilog;
using Stratum.Droid.Callback;
using Stratum.Droid.Storage;
using Stratum.Droid.Util;

namespace Stratum.Droid.Interface.Fragment
{
    public class UnlockBottomSheet : BottomSheet
    {
        private readonly ILogger _log = Log.ForContext<UnlockBottomSheet>();
        
        private PreferenceWrapper _preferences;
        private bool _canUseBiometrics;

        private MaterialButton _unlockButton;
        private MaterialButton _useBiometricsButton;
        private TextInputLayout _passwordLayout;
        private TextInputEditText _passwordText;
        private CircularProgressIndicator _progressIndicator;

        public UnlockBottomSheet() : base(Resource.Layout.sheetUnlock, Resource.String.unlock)
        {
        }

        public event EventHandler<string> UnlockAttempted;
        public event EventHandler Cancelled;

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var backPressCallback = new BackPressCallback(true);
            backPressCallback.BackPressed += delegate
            {
                Dismiss();
                Cancelled?.Invoke(this, EventArgs.Empty);
            };

            var dialog = (BottomSheetDialog) base.OnCreateDialog(savedInstanceState);
            dialog.OnBackPressedDispatcher.AddCallback(this, backPressCallback);
            return dialog;
        }

        public override void OnCancel(IDialogInterface dialog)
        {
            base.OnCancel(dialog);
            Cancelled?.Invoke(this, EventArgs.Empty); 
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetCancelable(false);

            _preferences = new PreferenceWrapper(Context);

            _progressIndicator = view.FindViewById<CircularProgressIndicator>(Resource.Id.progressIndicator);
            _passwordLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPasswordLayout);
            _passwordText = view.FindViewById<TextInputEditText>(Resource.Id.editPassword);
            TextInputUtil.EnableAutoErrorClear(_passwordLayout);

            _passwordText.EditorAction += (_, e) =>
            {
                if (e.ActionId == ImeAction.Done)
                {
                    UnlockAttempted?.Invoke(this, _passwordText.Text);
                }
            };

            _unlockButton = view.FindViewById<MaterialButton>(Resource.Id.buttonUnlock);
            _unlockButton.Click += delegate { UnlockAttempted?.Invoke(this, _passwordText.Text); };
            
            _useBiometricsButton = view.FindViewById<MaterialButton>(Resource.Id.buttonUseBiometrics);

            // Biometric support is only available on Android 6.0 (API 23) and above.
            if (_preferences.AllowBiometrics && Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                var biometricManager = BiometricManager.From(RequireContext());
                _canUseBiometrics = biometricManager.CanAuthenticate(BiometricManager.Authenticators.BiometricStrong) ==
                                    BiometricManager.BiometricSuccess;

                if (!_canUseBiometrics)
                {
                    Toast.MakeText(Context, Resource.String.biometricsChanged, ToastLength.Long).Show();
                }
            }
            else
            {
                _useBiometricsButton.Visibility = ViewStates.Gone;
            }

            _useBiometricsButton.Enabled = _canUseBiometrics;
            _useBiometricsButton.Click += delegate { ShowBiometricPrompt(); };

            if (_canUseBiometrics)
            {
                ShowBiometricPrompt();
            }
            else
            {
                FocusPasswordText();
            }

            return view;
        }

        public void SetLoading(bool loading)
        {
            if (loading)
            {
                _unlockButton.Visibility = ViewStates.Invisible;
                _progressIndicator.Visibility = ViewStates.Visible;
                _useBiometricsButton.Enabled = false;
            }
            else
            {
                _unlockButton.Visibility = ViewStates.Visible;
                _progressIndicator.Visibility = ViewStates.Invisible;
                _useBiometricsButton.Enabled = _canUseBiometrics;
            }
        }

        public void ShowError()
        {
            FocusPasswordText();
            _passwordLayout.Error = GetString(Resource.String.passwordIncorrect);
        }

        private void FocusPasswordText()
        {
            if (!_passwordText.RequestFocus())
            {
                return;
            }

            var inputManager = (InputMethodManager) Context.GetSystemService(Context.InputMethodService);
            inputManager.ShowSoftInput(_passwordText, ShowFlags.Implicit);
        }

        private void ShowBiometricPrompt()
        {
            // Biometric prompt is not supported on versions below Android 6.0 (API 23).
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            {
                return;
            }

            var executor = ContextCompat.GetMainExecutor(RequireContext());
            var passwordStorage = new BiometricStorage(Context);
            var callback = new AuthenticationCallback();

            callback.Succeeded += (_, result) =>
            {
                string password;

                try
                {
                    password = passwordStorage.Fetch(result.CryptoObject.Cipher);
                }
                catch (Exception e)
                {
                    _log.Error(e, "Failed to fetch biometrics cipher");
                    Toast.MakeText(Context, Resource.String.genericError, ToastLength.Short);
                    return;
                }

                UnlockAttempted?.Invoke(this, password);
            };

            callback.Failed += delegate { FocusPasswordText(); };

            callback.Errored += (_, result) =>
            {
                Toast.MakeText(Context, result.Message, ToastLength.Short).Show();
                FocusPasswordText();
            };

            var prompt = new BiometricPrompt(this, executor, callback);

            var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                .SetTitle(GetString(Resource.String.unlock))
                .SetSubtitle(GetString(Resource.String.unlockBiometricsMessage))
                .SetNegativeButtonText(GetString(Resource.String.cancel))
                .SetConfirmationRequired(false)
                .SetAllowedAuthenticators(BiometricManager.Authenticators.BiometricStrong)
                .Build();

            try
            {
                var cipher = passwordStorage.GetDecryptionCipher();
                prompt.Authenticate(promptInfo, new BiometricPrompt.CryptoObject(cipher));
            }
            catch (Exception e)
            {
                _log.Error(e, "Failed to authenticate");
                _canUseBiometrics = false;
                _useBiometricsButton.Enabled = false;
                FocusPasswordText();
            }
        }
    }
}