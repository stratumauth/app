// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Droid.Interface;

namespace Stratum.Droid.Persistence.View.Impl
{
    public class AuthenticatorView : IAuthenticatorView
    {
        private readonly IAuthenticatorRepository _authenticatorRepository;
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;

        private string _search;
        private CategorySelector _categorySelector;
        
        private SortMode _sortMode;

        private IEnumerable<Authenticator> _all;
        private List<Authenticator> _view;
        private IEnumerable<AuthenticatorCategory> _authenticatorCategories;

        public AuthenticatorView(IAuthenticatorRepository authenticatorRepository,
            IAuthenticatorCategoryRepository authenticatorCategoryRepository)
        {
            _authenticatorRepository = authenticatorRepository;
            _authenticatorCategoryRepository = authenticatorCategoryRepository;
            _categorySelector = CategorySelector.Of(MetaCategory.All);
            _view = new List<Authenticator>();
        }

        public string Search
        {
            get => _search;
            set
            {
                _search = value;
                Update();
            }
        }

        public CategorySelector CategorySelector
        {
            get => _categorySelector;
            set
            {
                _categorySelector = value;
                Update();
            }
        }

        public SortMode SortMode
        {
            get => _sortMode;
            set
            {
                _sortMode = value;
                Update();
            }
        }

        public async Task LoadFromPersistenceAsync()
        {
            _all = await _authenticatorRepository.GetAllAsync();
            _authenticatorCategories = await _authenticatorCategoryRepository.GetAllAsync();

            Update();
        }

        public void Update()
        {
            if (_all == null || _categorySelector == null)
            {
                return;
            }
            
            var view = _all.AsEnumerable();

            if (_categorySelector.IsMetaCategory(out var metaCategory))
            {
                if (metaCategory == MetaCategory.Uncategorised)
                {
                    view = _all.Where(a => _authenticatorCategories.All(c => c.AuthenticatorSecret != a.Secret));
                }
            }
            else if (_categorySelector.IsCategory(out var categoryId))
            {
                var bindingForCategory = _authenticatorCategories.Where(b => b.CategoryId == categoryId);
                view = _all.Where(a => bindingForCategory.Any(c => c.AuthenticatorSecret == a.Secret));
            }

            if (!string.IsNullOrEmpty(Search))
            {
                view = view.Where(i =>
                    i.Issuer.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    (i.Username != null && i.Username.Contains(Search, StringComparison.OrdinalIgnoreCase)));
            }

            if (SortMode == SortMode.Custom)
            {
                if (_categorySelector.IsCategory(out var categoryId))
                {
                    var bindingForCategory = _authenticatorCategories.Where(b => b.CategoryId == categoryId);
                    view = view.OrderBy(a => bindingForCategory.First(c => c.AuthenticatorSecret == a.Secret).Ranking);
                }
                else
                {
                    view = view.OrderBy(a => a.Ranking);
                }
            }
            else
            {
                view = SortMode switch
                {
                    SortMode.AlphabeticalAscending => view.OrderBy(a => a.Issuer).ThenBy(a => a.Username),
                    SortMode.AlphabeticalDescending => view.OrderByDescending(a => a.Issuer).ThenByDescending(a => a.Username),
                    SortMode.CopyCountAscending => view.OrderBy(a => a.CopyCount).ThenBy(a => a.Issuer),
                    SortMode.CopyCountDescending => view.OrderByDescending(a => a.CopyCount).ThenBy(a => a.Issuer),
                    _ => view
                };
            }

            _view = view.ToList();
        }

        public void Swap(int oldPosition, int newPosition)
        {
            var atOldPosition = _view.ElementAtOrDefault(newPosition);

            if (atOldPosition == null)
            {
                throw new ArgumentOutOfRangeException(nameof(oldPosition), "No authenticator at position");
            }

            var atNewPosition = _view.ElementAtOrDefault(oldPosition);

            if (atNewPosition == null)
            {
                throw new ArgumentOutOfRangeException(nameof(newPosition), "No authenticator at position");
            }

            _view[newPosition] = atNewPosition;
            _view[oldPosition] = atOldPosition;
        }

        public IEnumerator<Authenticator> GetEnumerator()
        {
            return _view.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _view).GetEnumerator();
        }

        public int Count => _view.Count;

        public Authenticator this[int index] => _view[index];

        public bool AnyWithoutFilter()
        {
            return _all != null && _all.Any();
        }

        public int IndexOf(Authenticator auth)
        {
            return _view.FindIndex(a => a.Secret == auth.Secret);
        }

        public void CommitRanking()
        {
            if (_categorySelector.IsMetaCategory(out var metaCategory))
            {
                if (metaCategory == MetaCategory.All)
                {
                    for (var i = 0; i < _view.Count; ++i)
                    {
                        _view[i].Ranking = i;
                    }
                }
                else if (metaCategory == MetaCategory.Uncategorised)
                {
                    throw new InvalidOperationException("Cannot reorder uncategorised authenticators");
                }
            }
            else if (_categorySelector.IsCategory(out var categoryId))
            {
                for (var i = 0; i < _view.Count; ++i)
                {
                    var auth = _view[i];
                    var binding = _authenticatorCategories.First(
                        ac => ac.AuthenticatorSecret == auth.Secret && ac.CategoryId == categoryId);
                    binding.Ranking = i;
                }
            }
        }

        public IEnumerable<AuthenticatorCategory> GetCurrentBindings()
        {
            if (_categorySelector.IsCategory(out var id))
            {
                return _authenticatorCategories.Where(ac => ac.CategoryId == id);
            }

            throw new InvalidOperationException("No category selected");
        }
    }
}