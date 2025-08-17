// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stratum.Core.Entity;

namespace Stratum.Core.Persistence
{
    public interface
        IAuthenticatorCategoryRepository : IAsyncRepository<AuthenticatorCategory, ValueTuple<string, string>>
    {
        Task<List<AuthenticatorCategory>> GetAllForAuthenticatorAsync(Authenticator authenticator);
        Task<List<AuthenticatorCategory>> GetAllForCategoryAsync(Category category);
        Task DeleteAllForAuthenticatorAsync(Authenticator authenticator);
        Task DeleteAllForCategoryAsync(Category category);
        Task TransferCategoryAsync(Category initial, Category next);
        Task TransferAuthenticatorAsync(Authenticator initial, Authenticator next);
    }
}