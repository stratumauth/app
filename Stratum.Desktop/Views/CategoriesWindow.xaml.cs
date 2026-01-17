// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autofac;
using Serilog;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;

namespace Stratum.Desktop.Views
{
    public partial class CategoriesWindow : Window
    {
        private readonly ILogger _log = Log.ForContext<CategoriesWindow>();
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;
        private ObservableCollection<Category> _categories;

        public CategoriesWindow()
        {
            InitializeComponent();
            _categoryRepository = App.Container.Resolve<ICategoryRepository>();
            _authenticatorCategoryRepository = App.Container.Resolve<IAuthenticatorCategoryRepository>();
            _categories = new ObservableCollection<Category>();
            CategoriesListBox.ItemsSource = _categories;
            Loaded += CategoriesWindow_Loaded;
        }

        private async void CategoriesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var cats = await _categoryRepository.GetAllAsync();
                _categories.Clear();
                foreach (var cat in cats.OrderBy(c => c.Ranking))
                {
                    _categories.Add(cat);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to load categories");
                MessageBox.Show($"Failed to load categories: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NewCategoryTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a category name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var category = new Category(name);
                await _categoryRepository.CreateAsync(category);
                _categories.Add(category);
                NewCategoryTextBox.Clear();
                _log.Information("Created category {Name}", name);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to create category");
                MessageBox.Show($"Failed to create category: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Category category)
            {
                var newName = Microsoft.VisualBasic.Interaction.InputBox("Enter new name:", "Rename Category", category.Name);
                if (!string.IsNullOrEmpty(newName) && newName != category.Name)
                {
                    RenameCategory(category, newName);
                }
            }
        }

        private async void RenameCategory(Category oldCategory, string newName)
        {
            try
            {
                var newCategory = new Category(newName) { Ranking = oldCategory.Ranking };
                await _categoryRepository.CreateAsync(newCategory);
                await _authenticatorCategoryRepository.TransferCategoryAsync(oldCategory, newCategory);
                await _categoryRepository.DeleteAsync(oldCategory);

                var index = _categories.IndexOf(oldCategory);
                if (index >= 0)
                {
                    _categories[index] = newCategory;
                }
                _log.Information("Renamed category from {OldName} to {NewName}", oldCategory.Name, newName);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to rename category");
                MessageBox.Show($"Failed to rename category: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Category category)
            {
                var result = MessageBox.Show(
                    $"Delete category '{category.Name}'?\nAuthenticators in this category will not be deleted.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _authenticatorCategoryRepository.DeleteAllForCategoryAsync(category);
                        await _categoryRepository.DeleteAsync(category);
                        _categories.Remove(category);
                        _log.Information("Deleted category {Name}", category.Name);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Failed to delete category");
                        MessageBox.Show($"Failed to delete category: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
