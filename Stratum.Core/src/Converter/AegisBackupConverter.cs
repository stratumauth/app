// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using SimpleBase;
using Stratum.Core.Backup;
using Stratum.Core.Entity;
using Stratum.Core.Generator;
using Stratum.Core.Util;

namespace Stratum.Core.Converter
{
    public class AegisBackupConverter : BackupConverter
    {
        private const string BaseAlgorithm = "AES";
        private const string Mode = "GCM";
        private const string Padding = "NoPadding";
        private const string AlgorithmDescription = BaseAlgorithm + "/" + Mode + "/" + Padding;

        private const int KeyLength = 32;

        private readonly ICustomIconDecoder _customIconDecoder;

        public AegisBackupConverter(IIconResolver iconResolver, ICustomIconDecoder customIconDecoder) : base(
            iconResolver)
        {
            _customIconDecoder = customIconDecoder;
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Maybe;

        public override async Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            AegisBackup<DecryptedDatabase> backup;

            if (string.IsNullOrEmpty(password))
            {
                backup = JsonSerializer.Deserialize<AegisBackup<DecryptedDatabase>>(data);
            }
            else
            {
                var encryptedBackup = JsonSerializer.Deserialize<AegisBackup<string>>(data);
                backup = await Task.Run(() => DecryptBackup(encryptedBackup, password));
            }

            if (backup.Version != 1)
            {
                throw new NotSupportedException("Unsupported backup version");
            }

            return await ConvertDatabaseAsync(backup.Database);
        }

        private static byte[] GetAuthenticatedBytes(byte[] payload, byte[] mac)
        {
            var result = new byte[payload.Length + mac.Length];
            Buffer.BlockCopy(payload, 0, result, 0, payload.Length);
            Buffer.BlockCopy(mac, 0, result, payload.Length, mac.Length);
            return result;
        }

        private static byte[] DecryptAesGcm(byte[] key, byte[] iv, byte[] data, byte[] mac)
        {
            var keyParameter = new ParametersWithIV(new KeyParameter(key), iv);
            var cipher = CipherUtilities.GetCipher(AlgorithmDescription);
            cipher.Init(false, keyParameter);

            var authenticatedBytes = GetAuthenticatedBytes(data, mac);
            return cipher.DoFinal(authenticatedBytes);
        }

        private static AegisBackup<DecryptedDatabase> DecryptBackup(AegisBackup<string> backup, string password)
        {
            var masterKey = GetMasterKeyFromSlots(backup.Header.Slots, password);

            var databaseBytes = Convert.FromBase64String(backup.Database);
            var ivBytes = Hex.Decode(backup.Header.Params.Nonce);
            var macBytes = Hex.Decode(backup.Header.Params.Tag);

            var decryptedBytes = DecryptAesGcm(masterKey, ivBytes, databaseBytes, macBytes);
            var database = JsonSerializer.Deserialize<DecryptedDatabase>(decryptedBytes);

            return new AegisBackup<DecryptedDatabase>
            {
                Version = backup.Version, Header = backup.Header, Database = database
            };
        }

        private static byte[] GetMasterKeyFromSlots(IEnumerable<Slot> slots, string password)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            foreach (var slot in slots.Where(slot => slot.Type == SlotType.Password))
            {
                try
                {
                    return DecryptSlot(slot, passwordBytes);
                }
                catch (InvalidCipherTextException)
                {
                    // Cannot be decrypted with the provided password
                }
            }

            throw new BackupPasswordException("Master key cannot be decrypted");
        }

        private static byte[] DecryptSlot(Slot slot, byte[] password)
        {
            var saltBytes = Hex.Decode(slot.Salt);
            var derivedKey = SCrypt.Generate(password, saltBytes, slot.N, slot.R, slot.P, KeyLength);

            var ivBytes = Hex.Decode(slot.KeyParams.Nonce);
            var keyBytes = Hex.Decode(slot.Key);
            var macBytes = Hex.Decode(slot.KeyParams.Tag);

            return DecryptAesGcm(derivedKey, ivBytes, keyBytes, macBytes);
        }

        private async Task<ConversionResult> ConvertDatabaseAsync(DecryptedDatabase database)
        {
            var authenticators = new List<Authenticator>();
            var categories = new List<Category>();
            var bindings = new List<AuthenticatorCategory>();
            var icons = new List<CustomIcon>();
            var failures = new List<ConversionFailure>();

            foreach (var entry in database.Entries)
            {
                Authenticator auth;

                try
                {
                    auth = entry.Convert(IconResolver);
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure { Description = entry.Issuer, Error = e.Message });
                    continue;
                }

                if (!string.IsNullOrEmpty(entry.Group))
                {
                    var category = categories.FirstOrDefault(c => c.Name == entry.Group);

                    if (category == null)
                    {
                        category = new Category(entry.Group);
                        categories.Add(category);
                    }

                    var binding = new AuthenticatorCategory(auth.Secret, category.Id);
                    bindings.Add(binding);
                }

                if (entry.Icon != null)
                {
                    CustomIcon newIcon;

                    try
                    {
                        newIcon = await _customIconDecoder.DecodeAsync(entry.Icon, true);
                    }
                    catch (ArgumentException)
                    {
                        newIcon = null;
                    }

                    if (newIcon != null)
                    {
                        var icon = icons.FirstOrDefault(ic => ic.Id == newIcon.Id);

                        if (icon == null)
                        {
                            icon = newIcon;
                            icons.Add(newIcon);
                        }

                        auth.Icon = CustomIcon.Prefix + icon.Id;
                    }
                }

                authenticators.Add(auth);
            }

            var backup = new Backup.Backup
            {
                Authenticators = authenticators,
                Categories = categories,
                AuthenticatorCategories = bindings,
                CustomIcons = icons
            };

            return new ConversionResult { Failures = failures, Backup = backup };
        }

        private sealed class AegisBackup<T>
        {
            [JsonPropertyName("version")]
            public int Version { get; set; }

            [JsonPropertyName("header")]
            public Header Header { get; set; }

            [JsonPropertyName("db")]
            public T Database { get; set; }
        }

        private sealed class KeyParams
        {
            [JsonPropertyName("nonce")]
            public string Nonce { get; set; }

            [JsonPropertyName("tag")]
            public string Tag { get; set; }
        }

        private sealed class Header
        {
            [JsonPropertyName("slots")]
            public List<Slot> Slots { get; set; }

            [JsonPropertyName("params")]
            public KeyParams Params { get; set; }
        }

        private enum SlotType
        {
            Raw = 0,
            Password = 1,
            Biometric = 2
        }

        private sealed class Slot
        {
            [JsonPropertyName("type")]
            public SlotType Type { get; set; }

            [JsonPropertyName("key")]
            public string Key { get; set; }

            [JsonPropertyName("key_params")]
            public KeyParams KeyParams { get; set; }

            [JsonPropertyName("salt")]
            public string Salt { get; set; }

            [JsonPropertyName("n")]
            public int N { get; set; }

            [JsonPropertyName("r")]
            public int R { get; set; }

            [JsonPropertyName("p")]
            public int P { get; set; }
        }

        private sealed class DecryptedDatabase
        {
            [JsonPropertyName("entries")]
            public List<Entry> Entries { get; set; }
        }

        private sealed class Entry
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("issuer")]
            public string Issuer { get; set; }

            [JsonPropertyName("group")]
            public string Group { get; set; }

            [JsonPropertyName("icon")]
            public byte[] Icon { get; set; }

            [JsonPropertyName("info")]
            public EntryInfo Info { get; set; }

            private string ConvertSecret(AuthenticatorType type)
            {
                var secret = Info.Secret;

                if (type == AuthenticatorType.MobileOtp)
                {
                    var secretBytes = Base32.Rfc4648.Decode(secret).ToArray();
                    secret = Hex.ToHexString(secretBytes);
                }

                return SecretUtil.Normalise(secret, type);
            }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                var type = Type switch
                {
                    "totp" => AuthenticatorType.Totp,
                    "hotp" => AuthenticatorType.Hotp,
                    "steam" => AuthenticatorType.SteamOtp,
                    "motp" => AuthenticatorType.MobileOtp,
                    "yandex" => AuthenticatorType.YandexOtp,
                    _ => throw new ArgumentException($"Type '{Type}' not supported")
                };

                var algorithm = Info.Algorithm switch
                {
                    "SHA1" => HashAlgorithm.Sha1,
                    "SHA256" => HashAlgorithm.Sha256,
                    "SHA512" => HashAlgorithm.Sha512,
                    // Unused field for this type
                    "MD5" when type == AuthenticatorType.MobileOtp => Authenticator.DefaultAlgorithm,
                    _ => throw new ArgumentException($"Algorithm '{Info.Algorithm}' not supported")
                };

                string issuer;
                string username;

                if (string.IsNullOrEmpty(Issuer))
                {
                    issuer = Name;
                    username = null;
                }
                else
                {
                    issuer = Issuer;
                    username = Name;
                }

                return new Authenticator
                {
                    Type = type,
                    Algorithm = algorithm,
                    Secret = ConvertSecret(type),
                    Digits = Info.Digits,
                    Period = Info.Period,
                    Issuer = issuer.Truncate(Authenticator.IssuerMaxLength),
                    Username = username.Truncate(Authenticator.UsernameMaxLength),
                    Counter = Info.Counter,
                    Icon = iconResolver.FindServiceKeyByName(issuer),
                    Pin = Info.Pin
                };
            }
        }

        private sealed class EntryInfo
        {
            [JsonPropertyName("secret")]
            public string Secret { get; set; }

            [JsonPropertyName("algo")]
            public string Algorithm { get; set; }

            [JsonPropertyName("digits")]
            public int Digits { get; set; }

            [JsonPropertyName("period")]
            public int Period { get; set; }

            [JsonPropertyName("counter")]
            public int Counter { get; set; }

            [JsonPropertyName("pin")]
            public string Pin { get; set; }
        }
    }
}