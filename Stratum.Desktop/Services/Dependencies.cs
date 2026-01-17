// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using Autofac;
using Stratum.Core;
using Stratum.Core.Comparer;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Service;
using Stratum.Core.Service.Impl;
using Stratum.Desktop.Persistence;
using Stratum.Desktop.ViewModels;

namespace Stratum.Desktop.Services
{
    public static class Dependencies
    {
        public static IContainer Build(Database database)
        {
            var builder = new ContainerBuilder();

            // Database
            builder.RegisterInstance(database).AsSelf();

            // Repositories
            builder.RegisterType<AuthenticatorRepository>().As<IAuthenticatorRepository>().SingleInstance();
            builder.RegisterType<CategoryRepository>().As<ICategoryRepository>().SingleInstance();
            builder.RegisterType<AuthenticatorCategoryRepository>().As<IAuthenticatorCategoryRepository>().SingleInstance();
            builder.RegisterType<CustomIconRepository>().As<ICustomIconRepository>().SingleInstance();
            builder.RegisterType<IconPackRepository>().As<IIconPackRepository>().SingleInstance();
            builder.RegisterType<IconPackEntryRepository>().As<IIconPackEntryRepository>().SingleInstance();

            // Comparers
            builder.RegisterType<AuthenticatorComparer>().As<IEqualityComparer<Authenticator>>().SingleInstance();
            builder.RegisterType<CategoryComparer>().As<IEqualityComparer<Category>>().SingleInstance();
            builder.RegisterType<AuthenticatorCategoryComparer>().As<IEqualityComparer<AuthenticatorCategory>>().SingleInstance();
            builder.RegisterType<CustomIconComparer>().As<IEqualityComparer<CustomIcon>>().SingleInstance();

            // Asset Provider
            builder.RegisterType<DesktopAssetProvider>().As<IAssetProvider>().SingleInstance();

            // Icon Resolver (for backup converters)
            builder.RegisterType<DesktopIconResolver>().As<IIconResolver>().SingleInstance();

            // Custom Icon Decoder (for importing icons)
            builder.RegisterType<DesktopCustomIconDecoder>().As<ICustomIconDecoder>().SingleInstance();

            // Services
            builder.RegisterType<AuthenticatorService>().As<IAuthenticatorService>().SingleInstance();
            builder.RegisterType<CategoryService>().As<ICategoryService>().SingleInstance();
            builder.RegisterType<CustomIconService>().As<ICustomIconService>().SingleInstance();
            builder.RegisterType<BackupService>().As<IBackupService>().SingleInstance();
            builder.RegisterType<RestoreService>().As<IRestoreService>().SingleInstance();
            builder.RegisterType<ImportService>().As<IImportService>().SingleInstance();
            builder.RegisterType<IconPackService>().As<IIconPackService>().SingleInstance();

            // Desktop-specific services
            builder.RegisterType<PreferenceManager>().AsSelf().SingleInstance();
            builder.RegisterType<IconResolver>().AsSelf().SingleInstance();

            // ViewModels
            builder.RegisterType<MainViewModel>().AsSelf();

            return builder.Build();
        }
    }
}
