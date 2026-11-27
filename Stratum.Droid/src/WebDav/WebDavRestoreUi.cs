// Copyright (C) 2026 muort
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Widget;
using Google.Android.Material.Dialog;
using Stratum.Droid.Activity;

namespace Stratum.Droid.WebDav
{
    /// <summary>
    /// 「从 WebDAV 恢复」：列出云端备份，选择后直接在 Stratum 内恢复，或写回本地备份目录。
    /// 上游只需要在 AddMenuBottomSheet 里加一行菜单、在 MainActivity 里订阅一次点击。
    /// </summary>
    public static class WebDavRestoreUi
    {
        public static async Task ShowAsync(MainActivity activity)
        {
            if (activity == null || activity.IsFinishing) return;

            var config = new WebDavConfig(activity);
            if (!config.IsConfigured)
            {
                Toast.MakeText(activity, Resource.String.webdavNotConfigured, ToastLength.Short).Show();
                return;
            }

            List<WebDavEntry> entries;
            try
            {
                Toast.MakeText(activity, Resource.String.webdavLoading, ToastLength.Short).Show();
                entries = await WebDavBackupService.ListAsync(activity);
            }
            catch (Exception e)
            {
                Toast.MakeText(activity, activity.GetString(Resource.String.webdavLoadFailed, e.Message),
                    ToastLength.Long).Show();
                return;
            }

            if (entries.Count == 0)
            {
                Toast.MakeText(activity, Resource.String.webdavNoBackups, ToastLength.Short).Show();
                return;
            }

            var labels = entries.Select(entry =>
            {
                var date = entry.LastModified?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "";
                return $"{entry.Name}  ·  {date}  {entry.DisplaySize}";
            }).ToArray();

            var adapter = new ArrayAdapter<string>(activity, Android.Resource.Layout.SimpleListItem1, labels);

            new MaterialAlertDialogBuilder(activity)
                .SetTitle(Resource.String.webdavRestoreTitle)
                .SetAdapter(adapter, (s, args) => ShowActions(activity, entries[args.Which]))
                .SetNegativeButton(Android.Resource.String.Cancel, delegate { })
                .Show();
        }

        private static void ShowActions(MainActivity activity, WebDavEntry entry)
        {
            new MaterialAlertDialogBuilder(activity)
                .SetTitle(entry.Name)
                .SetPositiveButton(Resource.String.webdavRestoreToApp,
                    async (s, args) => await RestoreToAppAsync(activity, entry))
                .SetNeutralButton(Resource.String.webdavDownloadToLocal,
                    async (s, args) => await DownloadToLocalAsync(activity, entry))
                .SetNegativeButton(Android.Resource.String.Cancel, delegate { })
                .Show();
        }

        private static async Task RestoreToAppAsync(MainActivity activity, WebDavEntry entry)
        {
            try
            {
                var data = await WebDavBackupService.DownloadBytesAsync(activity, entry);
                await activity.RestoreFromCloudAsync(data, entry.Name);
            }
            catch (Exception e)
            {
                Toast.MakeText(activity, activity.GetString(Resource.String.webdavRestoreFailed, e.Message),
                    ToastLength.Long).Show();
            }
        }

        private static async Task DownloadToLocalAsync(MainActivity activity, WebDavEntry entry)
        {
            try
            {
                await WebDavBackupService.DownloadToLocalBackupDirAsync(activity, entry);
                Toast.MakeText(activity, Resource.String.webdavDownloaded, ToastLength.Short).Show();
            }
            catch (Exception e)
            {
                Toast.MakeText(activity, activity.GetString(Resource.String.webdavRestoreFailed, e.Message),
                    ToastLength.Long).Show();
            }
        }
    }
}
