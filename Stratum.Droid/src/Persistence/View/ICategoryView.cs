// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Stratum.Core.Entity;

namespace Stratum.Droid.Persistence.View
{
    public interface ICategoryView : IReorderableView<Category>
    {
        Task LoadFromPersistenceAsync();
        int IndexOf(string id);
    }
}