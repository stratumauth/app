// Copyright (C) 2026 muort
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Stratum.Droid.Storage;

namespace Stratum.Droid.WebDav
{
    /// <summary>
    /// WebDAV 配置。服务器信息放普通 SharedPreferences，密码放 Android Keystore 加密的 SecureStorage。
    /// 该文件是独立新增的，不修改上游任何偏好键。
    /// </summary>
    public sealed class WebDavConfig
    {
        public const string PreferenceKey = "pref_stratum_webdav";

        private const string PreferencesName = "stratum_webdav";
        private const string PasswordKey = "webdavPassword";
        private const string UrlKey = "url";
        private const string DirectoryKey = "directory";
        private const string UsernameKey = "username";
        private const string TrustAllKey = "trustAll";
        private const string EnabledKey = "enabled";
        private const string LastStatusKey = "lastStatus";

        private readonly ISharedPreferences _preferences;
        private readonly SecureStorage _secureStorage;

        public WebDavConfig(Context context)
        {
            _preferences = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private);
            _secureStorage = new SecureStorage(context);
        }

        public bool Enabled
        {
            get => _preferences.GetBoolean(EnabledKey, false);
            set => _preferences.Edit().PutBoolean(EnabledKey, value).Commit();
        }

        public string Url
        {
            get => _preferences.GetString(UrlKey, null);
            set => _preferences.Edit().PutString(UrlKey, value?.Trim()).Commit();
        }

        public string Directory
        {
            get
            {
                var value = _preferences.GetString(DirectoryKey, "Stratum");
                return string.IsNullOrWhiteSpace(value) ? "Stratum" : value.Trim().Trim('/');
            }
            set => _preferences.Edit().PutString(DirectoryKey, value?.Trim().Trim('/')).Commit();
        }

        public string Username
        {
            get => _preferences.GetString(UsernameKey, null);
            set => _preferences.Edit().PutString(UsernameKey, value).Commit();
        }

        public string Password
        {
            get => _secureStorage.Get(PasswordKey);
            set => _secureStorage.Set(PasswordKey, value);
        }

        public bool TrustAllCertificates
        {
            get => _preferences.GetBoolean(TrustAllKey, false);
            set => _preferences.Edit().PutBoolean(TrustAllKey, value).Commit();
        }

        public string LastStatus
        {
            get => _preferences.GetString(LastStatusKey, null);
            set => _preferences.Edit().PutString(LastStatusKey, value).Commit();
        }

        public bool IsConfigured =>
            Enabled && !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(Username);

        public string GetSummary(Context context)
        {
            if (!Enabled) return context.GetString(Resource.String.webdavSummaryDisabled);
            if (string.IsNullOrWhiteSpace(Url)) return context.GetString(Resource.String.webdavSummaryNotConfigured);
            return $"{Url.TrimEnd('/')}/{Directory}";
        }
    }
}
