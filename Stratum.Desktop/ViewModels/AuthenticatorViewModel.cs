// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows.Media;
using Autofac;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Generator;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.ViewModels
{
    public class AuthenticatorViewModel : INotifyPropertyChanged
    {
        private readonly Timer _copiedFeedbackTimer;
        private readonly IconResolver _iconResolver;
        private string _code;
        private int _timeRemaining;
        private bool _justCopied;

        public event PropertyChangedEventHandler PropertyChanged;

        public AuthenticatorViewModel(Authenticator auth)
        {
            Auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _iconResolver = App.Container.Resolve<IconResolver>();
            _copiedFeedbackTimer = new Timer(2000);
            _copiedFeedbackTimer.Elapsed += CopiedFeedbackTimer_Elapsed;
            _copiedFeedbackTimer.AutoReset = false;
            UpdateCode();
        }

        public Authenticator Auth { get; }

        public string Issuer => Auth.Issuer ?? "Unknown";
        public string Username => Auth.Username;
        public bool HasUsername => !string.IsNullOrEmpty(Auth.Username);
        public string IssuerInitial => !string.IsNullOrEmpty(Auth.Issuer) ? Auth.Issuer[0].ToString().ToUpper() : "?";
        public string Icon => Auth.Icon;
        public ImageSource IconImage => _iconResolver.GetIcon(Auth);
        public int Period => Auth.Period;
        public bool IsTimeBased => Auth.Type.GetGenerationMethod() == GenerationMethod.Time;
        public bool IsCounterBased => Auth.Type.GetGenerationMethod() == GenerationMethod.Counter;
        public AuthenticatorType Type => Auth.Type;

        public string Code
        {
            get => _code;
            private set
            {
                if (_code != value)
                {
                    _code = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormattedCode));
                }
            }
        }

        public string FormattedCode
        {
            get
            {
                if (string.IsNullOrEmpty(_code))
                {
                    return "------";
                }

                // Format code with spaces (e.g., "123 456" or "1234 5678")
                if (_code.Length <= 6)
                {
                    return $"{_code.Substring(0, _code.Length / 2)} {_code.Substring(_code.Length / 2)}";
                }
                else if (_code.Length == 8)
                {
                    return $"{_code.Substring(0, 4)} {_code.Substring(4)}";
                }
                return _code;
            }
        }

        public int TimeRemaining
        {
            get => _timeRemaining;
            private set
            {
                if (_timeRemaining != value)
                {
                    _timeRemaining = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool JustCopied
        {
            get => _justCopied;
            private set
            {
                if (_justCopied != value)
                {
                    _justCopied = value;
                    OnPropertyChanged();
                }
            }
        }

        public void RefreshFromAuth()
        {
            OnPropertyChanged(nameof(Issuer));
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(HasUsername));
            OnPropertyChanged(nameof(IssuerInitial));
            OnPropertyChanged(nameof(Icon));
            OnPropertyChanged(nameof(IconImage));
        }

        public void UpdateCode()
        {
            try
            {
                if (IsTimeBased)
                {
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    TimeRemaining = Auth.Period - (int)(now % Auth.Period);
                }

                Code = Auth.GetCode();
            }
            catch (Exception)
            {
                Code = "Error";
                TimeRemaining = 0;
            }
        }

        public void ShowCopiedFeedback()
        {
            JustCopied = true;
            _copiedFeedbackTimer.Stop();
            _copiedFeedbackTimer.Start();
        }

        private void CopiedFeedbackTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                JustCopied = false;
            });
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
