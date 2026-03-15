// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Timers;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Wear.Widget;
using Java.Lang;
using Stratum.Core;
using Stratum.Core.Generator;
using Stratum.Core.Util;
using Stratum.Droid.Shared;
using Stratum.WearOS.Callback;
using Stratum.WearOS.Interface;
using Stratum.WearOS.Util;

namespace Stratum.WearOS.Fragment
{
    public class CodeFragment() : AndroidX.Fragment.App.Fragment(Resource.Layout.fragmentCode)
    {
        private IGenerator _generator;
        private CodeGrouping _codeGroupSize;

        private AuthenticatorType _type;
        private string _issuer;
        private string _username;
        private int _period;
        private int _digits;
        private string _secret;
        private string _pin;
        private HashAlgorithm _algorithm;
        private string _icon;
        private Bitmap _customIcon;

        private AuthProgressLayout _authProgressLayout;
        private TextView _codeTextView;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var preferences = new PreferenceWrapper(RequireContext());
            _codeGroupSize = preferences.CodeGroupSize;

            _type = (AuthenticatorType) Arguments.GetInt("type");
            _username = Arguments.GetString("username");
            _issuer = Arguments.GetString("issuer");
            _period = Arguments.GetInt("period");
            _digits = Arguments.GetInt("digits");
            _secret = Arguments.GetString("secret");
            _pin = Arguments.GetString("pin");
            _algorithm = (HashAlgorithm) Arguments.GetInt("algorithm");
            
            var hasCustomIcon = Arguments.GetBoolean("hasCustomIcon");

            if (hasCustomIcon)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                {
#pragma warning disable CA1416
                    _customIcon = (Bitmap) Arguments.GetParcelable("icon", Class.FromType(typeof(Bitmap)));
#pragma warning restore CA1416
                }
                else
                {
#pragma warning disable CA1422
                    _customIcon = (Bitmap) Arguments.GetParcelable("icon");
#pragma warning restore CA1422
                }
            }
            else
            {
                _icon = Arguments.GetString("icon");
            }
            
            _generator = AuthenticatorUtil.GetGenerator(_type, _secret, _pin, _period, _algorithm, _digits);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            var swipeDismissLayout = view.FindViewById<SwipeDismissFrameLayout>(Resource.Id.layoutSwipeDismiss);
            
            var dismissCallback = new SwipeDismissCallback();
            dismissCallback.Dismiss += (_, _) =>
            {
                RequireActivity().SupportFragmentManager.PopBackStack();
                swipeDismissLayout.Visibility = ViewStates.Gone;      
            };

            swipeDismissLayout.AddCallback(dismissCallback);

            _authProgressLayout = view.FindViewById<AuthProgressLayout>(Resource.Id.layoutAuthProgress);
            _codeTextView = view.FindViewById<TextView>(Resource.Id.textCode);

            var issuerText = view.FindViewById<TextView>(Resource.Id.textIssuer);
            var usernameText = view.FindViewById<TextView>(Resource.Id.textUsername);

            issuerText.Text = _issuer;

            if (string.IsNullOrEmpty(_username))
            {
                usernameText.Visibility = ViewStates.Gone;
            }
            else
            {
                usernameText.Text = _username;
            }

            var iconView = view.FindViewById<ImageView>(Resource.Id.imageIcon);

            if (_customIcon != null)
            {
                iconView.SetImageBitmap(_customIcon);
            }
            else if (_icon != null)
            {
                iconView.SetImageResource(IconResolver.GetService(_icon, true));
            }
            else
            {
                iconView.SetImageResource(IconResolver.GetService(IconResolver.Default, true));
            }

            _authProgressLayout.Period = _period * 1000;
            _authProgressLayout.TimerFinished += Refresh;
        }

        public override void OnResume()
        {
            base.OnResume();

            if (_codeTextView != null && _authProgressLayout != null)
            {
                Refresh();
            }
        }

        public override void OnPause()
        {
            base.OnPause();
            _authProgressLayout?.StopTimer();
        }

        private void Refresh(object sender = null, ElapsedEventArgs args = null)
        {
            var (code, secondsRemaining) = AuthenticatorUtil.GetCodeAndRemainingSeconds(_generator, _period);
            _codeTextView.Text = CodeUtil.PadCode(code, _digits, _codeGroupSize);
            _authProgressLayout.StartTimer((_period - secondsRemaining) * 1000);
        }
    }
}