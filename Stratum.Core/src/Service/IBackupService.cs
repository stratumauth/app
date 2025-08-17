// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Stratum.Core.Backup;

namespace Stratum.Core.Service
{
    public interface IBackupService
    {
        Task<Backup.Backup> CreateBackupAsync();
        Task<HtmlBackup> CreateHtmlBackupAsync();
        Task<UriListBackup> CreateUriListBackupAsync();
    }
}