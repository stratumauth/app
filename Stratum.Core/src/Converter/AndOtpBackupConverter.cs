// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Stratum.Core.Backup;
using Stratum.Core.Entity;
using Stratum.Core.Generator;
using Stratum.Core.Util;

namespace Stratum.Core.Converter
{
    public class AndOtpBackupConverter : BackupConverter
    {
        private const string BaseAlgorithm = "AES";
        private const string Mode = "GCM";
        private const string Padding = "NoPadding";
        private const string AlgorithmDescription = BaseAlgorithm + "/" + Mode + "/" + Padding;

        private const int IterationsLength = 4;
        private const int SaltLength = 12;
        private const int IvLength = 12;
        private const int KeyLength = 32;


        public AndOtpBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Maybe;

        public override async Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var decryptedData = !string.IsNullOrEmpty(password)
                ? await Task.Run(() => Decrypt(data, password))
                : data;
            
            var sourceAccounts = JsonSerializer.Deserialize<List<Account>>(decryptedData);

            var authenticators = new List<Authenticator>();
            var categories = new List<Category>();
            var bindings = new List<AuthenticatorCategory>();
            var failures = new List<ConversionFailure>();

            foreach (var account in sourceAccounts)
            {
                Authenticator auth;

                try
                {
                    auth = account.Convert(IconResolver);
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure { Description = account.Issuer, Error = e.Message });

                    continue;
                }

                authenticators.Add(auth);

                foreach (var tag in account.Tags)
                {
                    var category = categories.FirstOrDefault(c => c.Name == tag);

                    if (category == null)
                    {
                        category = new Category(tag);
                        categories.Add(category);
                    }

                    var binding = new AuthenticatorCategory(auth.Secret, category.Id);
                    bindings.Add(binding);
                }
            }

            var backup = new Backup.Backup
            {
                Authenticators = authenticators, Categories = categories, AuthenticatorCategories = bindings
            };

            return new ConversionResult { Failures = failures, Backup = backup };
        }

        private static KeyParameter DeriveKey(string password, byte[] salt, uint iterations)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var generator = new Pkcs5S2ParametersGenerator(new Sha1Digest());
            generator.Init(passwordBytes, salt, (int) iterations);
            return (KeyParameter) generator.GenerateDerivedParameters(BaseAlgorithm, KeyLength * 8);
        }

        private static byte[] Decrypt(byte[] data, string password)
        {
            var iterations = BinaryPrimitives.ReadUInt32BigEndian(data.Take(IterationsLength).ToArray());
            var salt = data.Skip(IterationsLength).Take(SaltLength).ToArray();
            var iv = data.Skip(IterationsLength + SaltLength).Take(IvLength).ToArray();
            var payload = data.Skip(IterationsLength + SaltLength + IvLength).ToArray();

            var key = DeriveKey(password, salt, iterations);

            var keyParameter = new ParametersWithIV(key, iv);
            var cipher = CipherUtilities.GetCipher(AlgorithmDescription);
            cipher.Init(false, keyParameter);

            try
            {
                return cipher.DoFinal(payload);
            }
            catch (InvalidCipherTextException e)
            {
                throw new BackupPasswordException("The password is incorrect", e);
            }
        }

        private sealed class Account
        {
            [JsonPropertyName("secret")]
            public string Secret { get; set; }

            [JsonPropertyName("issuer")]
            public string Issuer { get; set; }

            [JsonPropertyName("label")]
            public string Label { get; set; }

            [JsonPropertyName("digits")]
            public int Digits { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("algorithm")]
            public string Algorithm { get; set; }

            [JsonPropertyName("thumbnail")]
            public string Thumbnail { get; set; }

            [JsonPropertyName("period")]
            public int? Period { get; set; }

            [JsonPropertyName("counter")]
            public int Counter { get; set; }

            [JsonPropertyName("tags")]
            public List<string> Tags { get; set; }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                var type = Type switch
                {
                    "TOTP" => AuthenticatorType.Totp,
                    "HOTP" => AuthenticatorType.Hotp,
                    "STEAM" => AuthenticatorType.SteamOtp,
                    _ => throw new ArgumentException($"Type '{Type}' not supported")
                };

                var algorithm = Algorithm switch
                {
                    "SHA1" => HashAlgorithm.Sha1,
                    "SHA256" => HashAlgorithm.Sha256,
                    "SHA512" => HashAlgorithm.Sha512,
                    _ => throw new ArgumentException($"Algorithm '{Algorithm}' not supported")
                };

                string issuer;
                string username;

                if (string.IsNullOrEmpty(Issuer))
                {
                    issuer = Label;
                    username = null;
                }
                else
                {
                    issuer = Issuer;
                    username = Label;
                }

                return new Authenticator
                {
                    Secret = SecretUtil.Normalise(Secret, type),
                    Issuer = issuer.Truncate(Authenticator.IssuerMaxLength),
                    Username = username.Truncate(Authenticator.UsernameMaxLength),
                    Digits = Digits,
                    Period = Period ?? type.GetDefaultPeriod(),
                    Counter = Counter,
                    Type = type,
                    Algorithm = algorithm,
                    Icon = iconResolver.FindServiceKeyByName(Thumbnail)
                };
            }
        }
    }
}