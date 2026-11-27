// Copyright (C) 2026 muort
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using AndroidX.DocumentFile.Provider;
using Serilog;
using Stratum.Core.Backup;
using Stratum.Droid.Util;

namespace Stratum.Droid.WebDav
{
    /// <summary>
    /// 把 WebDAV 能力接到上游的备份/恢复流程上。
    /// 上游只需要在 AutoBackupWorker.BackupToDir 写完后调用一次 TryUploadAsync。
    /// </summary>
    public static class WebDavBackupService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(WebDavBackupService));

        /// <summary>
        /// 尽力上传，任何失败都只记录状态，不影响本地备份结果。
        /// </summary>
        public static async Task<bool> TryUploadAsync(Context context, string fileName, byte[] data)
        {
            if (context == null || string.IsNullOrWhiteSpace(fileName) || data == null) return false;

            var config = new WebDavConfig(context);
            if (!config.IsConfigured)
            {
                return false;
            }

            try
            {
                using var client = new WebDavClient(config);
                await client.UploadAsync(fileName, data);
                config.LastStatus = $"已上传 {fileName}";
                Log.Information("WebDAV upload succeeded: {FileName}", fileName);
                return true;
            }
            catch (Exception e)
            {
                config.LastStatus = $"上传失败：{e.Message}";
                Log.Error(e, "WebDAV upload failed: {FileName}", fileName);
                return false;
            }
        }

        public static Task<List<WebDavEntry>> ListAsync(Context context)
        {
            var config = new WebDavConfig(context);
            if (!config.IsConfigured)
            {
                throw new InvalidOperationException("WebDAV 未启用或未配置");
            }

            return Task.Run(async delegate
            {
                using var client = new WebDavClient(config);
                return await client.ListAsync();
            });
        }

        public static Task TestAsync(Context context)
        {
            var config = new WebDavConfig(context);
            return Task.Run(async delegate
            {
                using var client = new WebDavClient(config);
                await client.TestAsync();
            });
        }

        public static async Task DownloadToLocalBackupDirAsync(Context context, WebDavEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var preferences = new PreferenceWrapper(context);
            var treeUri = preferences.AutoBackupUri;
            if (treeUri == null)
            {
                throw new InvalidOperationException("还没有设置自动备份的本地目录");
            }

            var config = new WebDavConfig(context);
            byte[] data;
            using (var client = new WebDavClient(config))
            {
                data = await client.DownloadAsync(entry);
            }

            var directory = DocumentFile.FromTreeUri(context, treeUri);
            if (directory == null)
            {
                throw new InvalidOperationException("无法打开本地备份目录");
            }

            var file = directory.CreateFile(Backup.MimeType, entry.Name);
            if (file == null)
            {
                throw new InvalidOperationException("无法在本地备份目录创建文件");
            }

            await FileUtil.WriteFileAsync(context, file.Uri, data);
            config.LastStatus = $"已下载 {entry.Name} 到本地备份目录";
        }

        public static async Task<byte[]> DownloadBytesAsync(Context context, WebDavEntry entry)
        {
            var config = new WebDavConfig(context);
            using var client = new WebDavClient(config);
            return await client.DownloadAsync(entry);
        }
    }
}
