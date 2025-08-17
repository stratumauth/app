// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;

namespace Stratum.Core
{
    public interface IAssetProvider
    {
        Task<byte[]> ReadBytesAsync(string path);
        Task<string> ReadStringAsync(string path);
    }
}