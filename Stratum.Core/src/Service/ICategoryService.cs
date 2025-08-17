// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stratum.Core.Entity;

namespace Stratum.Core.Service
{
    public interface ICategoryService
    {
        Task<Category> GetCategoryByIdAsync(string id);
        Task TransferAsync(Category initial, Category next);
        Task AddCategoryAsync(Category category);
        Task<int> AddManyCategoriesAsync(IEnumerable<Category> categories);
        Task<ValueTuple<int, int>> AddOrUpdateManyCategoriesAsync(IEnumerable<Category> categories);
        Task<int> UpdateManyCategoriesAsync(IEnumerable<Category> categories);
        Task<int> AddManyBindingsAsync(IEnumerable<AuthenticatorCategory> acs);
        Task<ValueTuple<int, int>> AddOrUpdateManyBindingsAsync(IEnumerable<AuthenticatorCategory> acs);
        Task<int> UpdateManyBindingsAsync(IEnumerable<AuthenticatorCategory> acs);
        Task AddBindingAsync(Authenticator authenticator, Category category);
        Task RemoveBindingAsync(Authenticator authenticator, Category category);
        Task DeleteWithCategoryBindingsASync(Category category);
        Task<List<AuthenticatorCategory>> GetBindingsForAuthenticatorAsync(Authenticator authenticator);
        Task<List<AuthenticatorCategory>> GetBindingsForCategoryAsync(Category category);
        Task<List<Category>> GetAllCategoriesAsync();
        Task<List<AuthenticatorCategory>> GetAllBindingsAsync();
    }
}