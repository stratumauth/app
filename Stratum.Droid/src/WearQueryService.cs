// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

#if !FDROID

using Android.App;
using Android.Gms.Wearable;
using Stratum.Droid.Shared.Wear;
using Stratum.Core.Service;
using Java.IO;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Stratum.Droid.Persistence.View;

namespace Stratum.Droid
{
    [Service(Exported = true)]
    [IntentFilter(
        new[] { ChannelApi.ActionChannelEvent },
        DataScheme = "wear",
        DataHost = "*"
    )]
    public class WearQueryService : WearableListenerService
    {
        private const string GetSyncBundleCapability = "get_sync_bundle";

        private readonly ILogger _log = Log.ForContext<WearQueryService>();
        private readonly Database _database;

        private readonly ICategoryService _categoryService;
        private readonly ICustomIconService _customIconService;
        private SecureStorageWrapper _secureStorageWrapper;

        public WearQueryService()
        {
            _database = Dependencies.Resolve<Database>();

            _categoryService = Dependencies.Resolve<ICategoryService>();
            _customIconService = Dependencies.Resolve<ICustomIconService>();
        }

        public override void OnCreate()
        {
            base.OnCreate();
            _secureStorageWrapper = new SecureStorageWrapper(this);
        }

        private Task<T> UseDatabaseAsync<T>(Func<Task<T>> action)
        {
            var password = _secureStorageWrapper.GetDatabasePassword();
            return _database.UseAsync(password, Database.Origin.Wear, action);
        }

        private async Task<byte[]> GetSyncBundleAsync()
        {
            var authenticatorView = Dependencies.Resolve<IAuthenticatorView>();
            await authenticatorView.LoadFromPersistenceAsync();
            var auths = new List<WearAuthenticator>();

            var authCategories = await _categoryService.GetAllBindingsAsync();

            foreach (var auth in authenticatorView)
            {
                var bindings = authCategories
                    .Where(c => c.AuthenticatorSecret == auth.Secret)
                    .Select(c => new WearAuthenticatorCategory { CategoryId = c.CategoryId, Ranking = c.Ranking })
                    .ToList();

                var item = new WearAuthenticator
                {
                    Type = auth.Type,
                    Secret = auth.Secret,
                    Pin = auth.Pin,
                    Icon = auth.Icon,
                    Issuer = auth.Issuer,
                    Username = auth.Username,
                    Period = auth.Period,
                    Digits = auth.Digits,
                    Algorithm = auth.Algorithm,
                    Ranking = auth.Ranking,
                    CopyCount = auth.CopyCount,
                    Categories = bindings
                };

                auths.Add(item);
            }

            var categories = (await _categoryService.GetAllCategoriesAsync())
                .Select(c => new WearCategory { Id = c.Id, Name = c.Name, Ranking = c.Ranking })
                .ToList();

            var customIcons = (await _customIconService.GetAllAsync())
                .Select(i => new WearCustomIcon { Id = i.Id, Data = i.Data })
                .ToList();

            var preferenceWrapper = new PreferenceWrapper(this);
            var preferences = new WearPreferences
            {
                DefaultCategory = preferenceWrapper.DefaultCategory,
                SortMode = preferenceWrapper.SortMode,
                CodeGroupSize = preferenceWrapper.CodeGroupSize,
                ShowUsernames = preferenceWrapper.ShowUsernames
            };

            var bundle = new WearSyncBundle
            {
                Authenticators = auths,
                Categories = categories,
                CustomIcons = customIcons,
                Preferences = preferences
            };

            return JsonSerializer.SerializeToUtf8Bytes(bundle);
        }

        private async Task SendSyncBundleAsync(ChannelClient.IChannel channel)
        {
            var client = WearableClass.GetChannelClient(this);
            var bundle = await UseDatabaseAsync(GetSyncBundleAsync);

            OutputStream stream = null;

            try
            {
                stream = await client.GetOutputStreamAsync(channel);
                await stream.WriteAsync(bundle);
            }
            finally
            {
                stream?.Close();
            }
        }

        public override async void OnChannelOpened(ChannelClient.IChannel channel)
        {
            _log.Debug("Wear channel opened: {Path}", channel.Path);

            if (channel.Path != GetSyncBundleCapability)
            {
                return;
            }

            try
            {
                await SendSyncBundleAsync(channel);
            }
            catch (Exception e)
            {
                _log.Error(e, "Error sending sync bundle");
            }
        }
    }
}

#endif