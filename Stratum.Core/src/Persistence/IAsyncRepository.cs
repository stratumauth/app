// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stratum.Core.Persistence
{
    public interface IAsyncRepository<T, in TU> where T : new()
    {
        Task CreateAsync(T item);
        Task<T> GetAsync(TU id);
        Task<List<T>> GetAllAsync();
        Task UpdateAsync(T item);
        Task DeleteAsync(T item);
    }
}