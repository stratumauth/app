// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Autofac;
using Stratum.WearOS.Cache;
using Stratum.WearOS.Cache.View;

namespace Stratum.WearOS
{
    internal static class Dependencies
    {
        private static IContainer _container;
        
        public static void Init(Context context)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(context).SingleInstance().ExternallyOwned();
            
            builder.RegisterType<AuthenticatorCache>().SingleInstance();
            builder.RegisterType<CategoryCache>().SingleInstance();
            builder.RegisterType<CustomIconCache>().SingleInstance();

            builder.RegisterType<AuthenticatorView>().SingleInstance();
            builder.RegisterType<CategoryView>().SingleInstance();

            _container = builder.Build();
        }

        public static T Resolve<T>() where T : class
        {
            return _container.Resolve<T>();
        }
    }
}