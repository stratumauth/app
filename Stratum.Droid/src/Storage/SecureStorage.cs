// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

// Adapted from Xamarin.Essentials (MIT License) for backwards compatibility
// https://github.com/xamarin/Essentials/blob/main/Xamarin.Essentials/SecureStorage/SecureStorage.android.cs

using System;
using System.Text;
using System.Threading;
using Android.Content;
using Android.Runtime;
using Android.Security.Keystore;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using Serilog;

namespace Stratum.Droid.Storage
{
    public class SecureStorage
    {
        private const string KeyStoreName = "AndroidKeyStore";
        private const string CipherDescription = "AES/GCM/NoPadding";
        private const int IvLength = 12; // Android supports an IV of 12 for AES/GCM

        private readonly ILogger _log = Log.ForContext<SecureStorage>();
        private readonly string _preferenceAlias;
        private readonly ISharedPreferences _preferences;
        private readonly Lock _lock = new();

        public SecureStorage(Context context)
        {
            _preferenceAlias = $"{context.PackageName}_securestorage";
            _preferences = context.GetSharedPreferences(_preferenceAlias, FileCreationMode.Private);
        }

        public string Get(string key)
        {
            var encryptedString = _preferences.GetString(key, null);

            if (encryptedString == null)
            {
                return null;
            }

            var encryptedBytes = Convert.FromBase64String(encryptedString);

            try
            {
                return Decrypt(encryptedBytes);
            }
            catch (AEADBadTagException e)
            {
                _log.Error(e, "Unable to decrypt value for key {Key}", key);
                _preferences.Edit().Remove(key).Commit();
                return null;
            }
        }

        public void Set(string key, string data)
        {
            if (data == null)
            {
                _preferences.Edit().Remove(key).Commit();
                return;
            }

            var encryptedData = Encrypt(data);
            var encryptedString = Convert.ToBase64String(encryptedData);

            _preferences.Edit().PutString(key, encryptedString).Commit();
        }

        private static KeyStore GetKeyStore()
        {
            var keyStore = KeyStore.GetInstance(KeyStoreName);
            keyStore.Load(null);

            return keyStore;
        }

        private ISecretKey GetKey()
        {
            IKey existingKey;

            lock (_lock)
            {
                var keyStore = GetKeyStore();
                existingKey = keyStore.GetKey(_preferenceAlias, null);
            }

            if (existingKey != null)
            {
                return existingKey.JavaCast<ISecretKey>();
            }

            lock (_lock)
            {
#pragma warning disable CA1416
                var keyGenerator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, KeyStoreName);

                var spec =
                    new KeyGenParameterSpec.Builder(_preferenceAlias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                        .SetBlockModes(KeyProperties.BlockModeGcm)
                        .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)
                        .SetRandomizedEncryptionRequired(false)
                        .Build();

                keyGenerator.Init(spec);
#pragma warning restore CA1416

                return keyGenerator.GenerateKey();
            }
        }

        private byte[] Encrypt(string data)
        {
            var key = GetKey();

            // Generate initialization vector
            var iv = new byte[IvLength];

            var secureRandom = new SecureRandom();
            secureRandom.NextBytes(iv);

            Cipher cipher;

            // Attempt to use GCMParameterSpec by default
            try
            {
                cipher = Cipher.GetInstance(CipherDescription);
                cipher.Init(CipherMode.EncryptMode, key, new GCMParameterSpec(128, iv));
            }
            catch (InvalidAlgorithmParameterException)
            {
                // If we encounter this error, it's likely an old bouncycastle provider version
                // is being used which does not recognize GCMParameterSpec, but should work
                // with IvParameterSpec, however we only do this as a last effort since other
                // implementations will error if you use IvParameterSpec when GCMParameterSpec
                // is recognized and expected.
                cipher = Cipher.GetInstance(CipherDescription);
                cipher.Init(CipherMode.EncryptMode, key, new IvParameterSpec(iv));
            }

            var decryptedData = Encoding.UTF8.GetBytes(data);
            var encryptedBytes = cipher.DoFinal(decryptedData);

            // Combine the IV and the encrypted data into one array
            var result = new byte[iv.Length + encryptedBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);

            return result;
        }

        private string Decrypt(byte[] data)
        {
            if (data.Length < IvLength)
            {
                return null;
            }

            var key = GetKey();

            // IV will be the first 16 bytes of the encrypted data
            var iv = new byte[IvLength];
            Buffer.BlockCopy(data, 0, iv, 0, IvLength);

            Cipher cipher;

            // Attempt to use GCMParameterSpec by default
            try
            {
                cipher = Cipher.GetInstance(CipherDescription);
                cipher.Init(CipherMode.DecryptMode, key, new GCMParameterSpec(128, iv));
            }
            catch (InvalidAlgorithmParameterException)
            {
                // If we encounter this error, it's likely an old bouncycastle provider version
                // is being used which does not recognize GCMParameterSpec, but should work
                // with IvParameterSpec, however we only do this as a last effort since other
                // implementations will error if you use IvParameterSpec when GCMParameterSpec
                // is recognized and expected.
                cipher = Cipher.GetInstance(CipherDescription);
                cipher.Init(CipherMode.DecryptMode, key, new IvParameterSpec(iv));
            }

            // Decrypt starting after the first 16 bytes from the IV
            var decryptedData = cipher.DoFinal(data, IvLength, data.Length - IvLength);

            return Encoding.UTF8.GetString(decryptedData);
        }
    }
}