// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using Android.Content;
using Autofac;
using Stratum.Core;
using Stratum.Core.Backup.Encryption;
using Stratum.Core.Comparer;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Service;
using Stratum.Core.Service.Impl;
using Stratum.Droid.Shared;
using Stratum.Droid.Interface;
using Stratum.Droid.Persistence;
using Stratum.Droid.Persistence.View;
using Stratum.Droid.Persistence.View.Impl;

namespace Stratum.Droid
{
    internal static class Dependencies
    {
        private static IContainer _container;

        public static void Init(Context context, Database database)
        {
            _container = CreateContainer(context, database);
        }

        public static IContainer CreateContainer(Context context, Database database)
        {
            var builder = new ContainerBuilder();
            RegisterAll(builder, context, database);
            return builder.Build();
        }

        private static void RegisterAll(ContainerBuilder builder, Context context, Database database)
        {
            builder.RegisterInstance(database).SingleInstance().ExternallyOwned();
            builder.RegisterInstance(context).SingleInstance().ExternallyOwned();
            
            builder.RegisterType<StrongBackupEncryption>().As<IBackupEncryption>().SingleInstance();
            builder.RegisterType<LegacyBackupEncryption>().As<IBackupEncryption>().SingleInstance();
            builder.RegisterType<NoBackupEncryption>().As<IBackupEncryption>().SingleInstance();

            builder.RegisterType<AssetProvider>().As<IAssetProvider>().SingleInstance();
            builder.RegisterType<CustomIconDecoder>().As<ICustomIconDecoder>().SingleInstance();
            builder.RegisterType<IconResolver>().As<IIconResolver>().SingleInstance();

            RegisterRepositories(builder);
            RegisterServices(builder);
            RegisterViews(builder);
        }

        private static void RegisterRepositories(ContainerBuilder builder)
        {
            builder.RegisterType<AuthenticatorRepository>().As<IAuthenticatorRepository>().SingleInstance();
            builder.RegisterType<CategoryRepository>().As<ICategoryRepository>().SingleInstance();
            builder.RegisterType<AuthenticatorCategoryRepository>().As<IAuthenticatorCategoryRepository>().SingleInstance();
            builder.RegisterType<CustomIconRepository>().As<ICustomIconRepository>().SingleInstance();
            builder.RegisterType<IconPackRepository>().As<IIconPackRepository>().SingleInstance();
            builder.RegisterType<IconPackEntryRepository>().As<IIconPackEntryRepository>().SingleInstance();
        }

        private static void RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterType<AuthenticatorComparer>().As<IEqualityComparer<Authenticator>>().SingleInstance();
            builder.RegisterType<CategoryComparer>().As<IEqualityComparer<Category>>().SingleInstance();
            builder.RegisterType<AuthenticatorCategoryComparer>().As<IEqualityComparer<AuthenticatorCategory>>().SingleInstance();

            builder.RegisterType<AuthenticatorService>().As<IAuthenticatorService>().SingleInstance();
            builder.RegisterType<BackupService>().As<IBackupService>().SingleInstance();
            builder.RegisterType<CategoryService>().As<ICategoryService>().SingleInstance();
            builder.RegisterType<CustomIconService>().As<ICustomIconService>().SingleInstance();
            builder.RegisterType<IconPackService>().As<IIconPackService>().SingleInstance();
            builder.RegisterType<ImportService>().As<IImportService>().SingleInstance();
            builder.RegisterType<RestoreService>().As<IRestoreService>().SingleInstance();
        }

        private static void RegisterViews(ContainerBuilder builder)
        {
            builder.RegisterType<AuthenticatorView>().As<IAuthenticatorView>();
            builder.RegisterType<CategoryView>().As<ICategoryView>();
            builder.RegisterType<CustomIconView>().As<ICustomIconView>();
            builder.RegisterType<DefaultIconView>().As<IDefaultIconView>();
            builder.RegisterType<IconPackEntryView>().As<IIconPackEntryView>();
            builder.RegisterType<IconPackView>().As<IIconPackView>();
        }

        public static T Resolve<T>() where T : class
        {
            return _container.Resolve<T>();
        }

        public static IEnumerable<T> ResolveAll<T>() where T : class
        {
            return _container.Resolve<IEnumerable<T>>();
        }
    }
}