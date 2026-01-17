// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILogger _log = Log.ForContext<MainViewModel>();
        private readonly IAuthenticatorRepository _authenticatorRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;
        private readonly PreferenceManager _preferenceManager;
        private readonly Timer _updateTimer;

        private string _searchText = "";
        private Category _selectedCategory;
        private ObservableCollection<AuthenticatorViewModel> _authenticators;
        private ObservableCollection<AuthenticatorViewModel> _filteredAuthenticators;
        private ObservableCollection<Category> _categories;
        private AuthenticatorViewModel _selectedAuthenticator;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel(
            IAuthenticatorRepository authenticatorRepository,
            ICategoryRepository categoryRepository,
            IAuthenticatorCategoryRepository authenticatorCategoryRepository,
            PreferenceManager preferenceManager)
        {
            _authenticatorRepository = authenticatorRepository;
            _categoryRepository = categoryRepository;
            _authenticatorCategoryRepository = authenticatorCategoryRepository;
            _preferenceManager = preferenceManager;

            _authenticators = new ObservableCollection<AuthenticatorViewModel>();
            _filteredAuthenticators = new ObservableCollection<AuthenticatorViewModel>();
            _categories = new ObservableCollection<Category>();

            _updateTimer = new Timer(500);
            _updateTimer.Elapsed += UpdateTimer_Elapsed;
            _updateTimer.AutoReset = true;

            InitializeCommands();
        }

        private void InitializeCommands()
        {
            AddAuthenticatorCommand = new RelayCommand(AddAuthenticator);
            EditAuthenticatorCommand = new RelayCommand<AuthenticatorViewModel>(EditAuthenticator);
            DeleteAuthenticatorCommand = new RelayCommand<AuthenticatorViewModel>(DeleteAuthenticator);
            CopyCodeCommand = new RelayCommand<AuthenticatorViewModel>(CopyCode);
            ShowQrCodeCommand = new RelayCommand<AuthenticatorViewModel>(ShowQrCode);
            IncrementCounterCommand = new RelayCommand<AuthenticatorViewModel>(IncrementCounter);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            FocusSearchCommand = new RelayCommand(FocusSearch);
            ClearSearchCommand = new RelayCommand(ClearSearch);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadAuthenticatorsAsync();
                await LoadCategoriesAsync();
                _updateTimer.Start();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to initialize MainViewModel");
                MessageBox.Show($"Failed to load data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadAuthenticatorsAsync()
        {
            var auths = await _authenticatorRepository.GetAllAsync();
            _authenticators.Clear();

            foreach (var auth in auths.OrderBy(a => a.Ranking))
            {
                _authenticators.Add(new AuthenticatorViewModel(auth));
            }

            ApplyFilter();
            OnPropertyChanged(nameof(AuthenticatorCount));
            OnPropertyChanged(nameof(IsEmpty));
        }

        private async Task LoadCategoriesAsync()
        {
            var cats = await _categoryRepository.GetAllAsync();
            _categories.Clear();
            _categories.Add(new Category { Id = null, Name = "All" });

            foreach (var cat in cats.OrderBy(c => c.Ranking))
            {
                _categories.Add(cat);
            }

            OnPropertyChanged(nameof(Categories));
        }

        private void ApplyFilter()
        {
            var filtered = _authenticators.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var search = _searchText.ToLowerInvariant();
                filtered = filtered.Where(a =>
                    a.Issuer.ToLowerInvariant().Contains(search) ||
                    (!string.IsNullOrEmpty(a.Username) && a.Username.ToLowerInvariant().Contains(search)));
            }

            if (_selectedCategory != null && !string.IsNullOrEmpty(_selectedCategory.Id))
            {
                var bindings = _authenticatorCategoryRepository.GetAllForCategoryAsync(_selectedCategory).GetAwaiter().GetResult();
                var secrets = new HashSet<string>(bindings.Select(b => b.AuthenticatorSecret));
                filtered = filtered.Where(a => secrets.Contains(a.Auth.Secret));
            }

            _filteredAuthenticators.Clear();
            foreach (var auth in filtered)
            {
                _filteredAuthenticators.Add(auth);
            }

            OnPropertyChanged(nameof(FilteredAuthenticators));
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                foreach (var auth in _authenticators)
                {
                    auth.UpdateCode();
                }
            });
        }

        private async void AddAuthenticator()
        {
            var dialog = new Views.AddAuthenticatorDialog
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                try
                {
                    await _authenticatorRepository.CreateAsync(dialog.Result);
                    _authenticators.Add(new AuthenticatorViewModel(dialog.Result));
                    ApplyFilter();
                    OnPropertyChanged(nameof(AuthenticatorCount));
                    OnPropertyChanged(nameof(IsEmpty));
                    _log.Information("Added authenticator for {Issuer}", dialog.Result.Issuer);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Failed to add authenticator");
                    MessageBox.Show($"Failed to add authenticator: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditAuthenticator(AuthenticatorViewModel auth)
        {
            if (auth == null) return;
            MessageBox.Show($"Edit {auth.Issuer} - not implemented yet", "Info");
        }

        private async void DeleteAuthenticator(AuthenticatorViewModel auth)
        {
            if (auth == null) return;

            var result = MessageBox.Show(
                $"Delete authenticator for {auth.Issuer}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _authenticatorRepository.DeleteAsync(auth.Auth);
                    await _authenticatorCategoryRepository.DeleteAllForAuthenticatorAsync(auth.Auth);
                    _authenticators.Remove(auth);
                    ApplyFilter();
                    OnPropertyChanged(nameof(AuthenticatorCount));
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Failed to delete authenticator");
                    MessageBox.Show($"Failed to delete: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CopyCode(AuthenticatorViewModel auth)
        {
            if (auth == null) return;

            try
            {
                Clipboard.SetText(auth.Code);
                auth.ShowCopiedFeedback();
                _log.Information("Copied code for {Issuer}", auth.Issuer);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to copy code");
            }
        }

        private void ShowQrCode(AuthenticatorViewModel auth)
        {
            if (auth == null) return;
            var dialog = new Views.QrCodeDialog(auth.Auth)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }

        private async void IncrementCounter(AuthenticatorViewModel auth)
        {
            if (auth == null) return;

            try
            {
                auth.Auth.Counter++;
                await _authenticatorRepository.UpdateAsync(auth.Auth);
                auth.UpdateCode();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to increment counter");
            }
        }

        private void OpenSettings()
        {
            var settingsWindow = new Views.SettingsWindow
            {
                Owner = Application.Current.MainWindow
            };
            settingsWindow.ShowDialog();
        }

        private void FocusSearch()
        {
            // This would focus the search box in the view
        }

        private void ClearSearch()
        {
            SearchText = "";
        }

        public void Dispose()
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Properties
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    ApplyFilter();
                }
            }
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    ApplyFilter();
                }
            }
        }

        public AuthenticatorViewModel SelectedAuthenticator
        {
            get => _selectedAuthenticator;
            set
            {
                if (_selectedAuthenticator != value)
                {
                    _selectedAuthenticator = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<AuthenticatorViewModel> FilteredAuthenticators => _filteredAuthenticators;
        public ObservableCollection<Category> Categories => _categories;
        public int AuthenticatorCount => _authenticators.Count;
        public bool IsEmpty => _filteredAuthenticators.Count == 0;

        // Commands
        public ICommand AddAuthenticatorCommand { get; private set; }
        public ICommand EditAuthenticatorCommand { get; private set; }
        public ICommand DeleteAuthenticatorCommand { get; private set; }
        public ICommand CopyCodeCommand { get; private set; }
        public ICommand ShowQrCodeCommand { get; private set; }
        public ICommand IncrementCounterCommand { get; private set; }
        public ICommand OpenSettingsCommand { get; private set; }
        public ICommand FocusSearchCommand { get; private set; }
        public ICommand ClearSearchCommand { get; private set; }
    }
}
