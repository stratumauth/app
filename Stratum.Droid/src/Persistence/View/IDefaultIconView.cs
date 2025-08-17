// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;

namespace Stratum.Droid.Persistence.View
{
    public interface IDefaultIconView : IView<KeyValuePair<string, int>>
    {
        string Search { get; set; }
        bool UseDarkTheme { get; set; }
    }
}