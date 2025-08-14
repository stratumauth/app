// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Text.Json;
using Android.OS;

namespace Stratum.Droid
{
    public static class ObjectBundler
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            IncludeFields = true
        };
        
        public static T GetObject<T>(this Bundle bundle, string key)
        {
            var data = bundle.GetByteArray(key);

            if (data == null)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(data, Options);
        }
        
        public static void PutObject<T>(this Bundle bundle, string key, T value)
        {
            bundle.PutByteArray(key, JsonSerializer.SerializeToUtf8Bytes(value));
        }
    }
}
