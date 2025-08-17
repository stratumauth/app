// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Droid.Interface;

namespace Stratum.Droid.Persistence.View
{
    public interface IAuthenticatorView : IReorderableView<Authenticator>
    {
        string Search { get; set; }
        CategorySelector CategorySelector { get; set; }
        SortMode SortMode { get; set; }
        Task LoadFromPersistenceAsync();
        bool AnyWithoutFilter();
        int IndexOf(Authenticator auth);
        IEnumerable<AuthenticatorCategory> GetCurrentBindings();
        void CommitRanking();
    }
}