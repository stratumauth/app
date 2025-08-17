// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Stratum.Core.Entity;

namespace Stratum.Core.Service
{
    public interface IIconPackService
    {
        Task ImportPackAsync(IconPack pack);
        Task DeletePackAsync(IconPack pack);
    }
}