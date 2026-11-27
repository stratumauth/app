// Copyright (C) 2026 muort
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Stratum.Core.Backup;
using Stratum.Core.Converter;

namespace Stratum.Droid.Activity
{
    /// <summary>
    /// WebDAV 一键还原的入口。单独放在 partial 文件里，MainActivity 上游文件只加 partial 关键字。
    /// 直接复用上游的 RestoreFromData / ImportFromData，密码弹窗、覆盖确认、重启都与原生一致。
    /// </summary>
    public partial class MainActivity
    {
        public async Task RestoreFromCloudAsync(byte[] data, string displayName)
        {
            if (data == null || data.Length == 0)
            {
                throw new InvalidOperationException("云端备份为空");
            }

            await UseLoadingScopeAsync(async delegate
            {
                if (displayName != null
                    && displayName.EndsWith("." + UriListBackup.FileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    await ImportFromData(new UriListBackupConverter(_iconResolver), data);
                }
                else if (displayName != null
                         && displayName.EndsWith("." + HtmlBackup.FileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    await ImportFromData(new HtmlBackupConverter(_iconResolver), data);
                }
                else
                {
                    await RestoreFromData(data);
                }
            });
        }
    }
}
