// Copyright (C) 2026 muort
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Content;
using AndroidX.Preference;
using Stratum.Droid.Interface.Fragment;

namespace Stratum.Droid.WebDav
{
    /// <summary>
    /// 在原生设置页的「通用」分类里注入一行「云备份（WebDAV）」。
    /// 上游只需要在 SettingsActivity.RegisterClickableEvents 里调用一次 Install。
    /// </summary>
    public static class WebDavSettingsEntry
    {
        private static WeakReference<Preference> _preference;

        public static void Install(SettingsFragment fragment, AndroidX.Fragment.App.FragmentManager manager,
            Context context)
        {
            if (fragment?.PreferenceScreen == null || context == null) return;

            var category = fragment.PreferenceScreen.FindPreference("pref_key_general") as PreferenceCategory;
            if (category == null) return;

            var preference = new Preference(context)
            {
                Key = WebDavConfig.PreferenceKey
            };
            preference.SetTitle(Resource.String.webdavSettingsTitle);
            preference.Summary = new WebDavConfig(context).GetSummary(context);

            preference.PreferenceClick += delegate
            {
                var sheet = new WebDavSettingsBottomSheet();
                sheet.Show(manager, sheet.Tag);
            };

            category.AddPreference(preference);
            _preference = new WeakReference<Preference>(preference);
        }

        public static void RefreshSummary(Context context)
        {
            if (context == null) return;

            if (_preference != null && _preference.TryGetTarget(out var preference))
            {
                preference.Summary = new WebDavConfig(context).GetSummary(context);
            }
        }
    }
}


