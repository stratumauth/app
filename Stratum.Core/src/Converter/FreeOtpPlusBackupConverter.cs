// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Stratum.Core.Util;
using SimpleBase;
using Stratum.Core.Backup;
using Stratum.Core.Entity;
using Stratum.Core.Generator;

namespace Stratum.Core.Converter
{
    public class FreeOtpPlusBackupConverter : BackupConverter
    {
        public FreeOtpPlusBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public override Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var sourceTokens = JsonSerializer.Deserialize<FreeOtpPlusBackup>(data).Tokens;
            var authenticators = new List<Authenticator>();
            var failures = new List<ConversionFailure>();

            foreach (var token in sourceTokens)
            {
                Authenticator auth;

                try
                {
                    auth = token.Convert(IconResolver);
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure { Description = token.Issuer, Error = e.Message });
                    continue;
                }

                authenticators.Add(auth);
            }

            var backup = new Backup.Backup { Authenticators = authenticators };
            var result = new ConversionResult { Failures = failures, Backup = backup };

            return Task.FromResult(result);
        }

        private sealed class FreeOtpPlusBackup
        {
            [JsonPropertyName("tokens")]
            public List<Token> Tokens { get; set; }
        }

        private sealed class Token
        {
            [JsonPropertyName("algo")]
            public string Algorithm { get; set; }

            [JsonPropertyName("counter")]
            public int Counter { get; set; }

            [JsonPropertyName("digits")]
            public int Digits { get; set; }

            [JsonPropertyName("issuerExt")]
            public string Issuer { get; set; }

            [JsonPropertyName("label")]
            public string Label { get; set; }

            [JsonPropertyName("period")]
            public int Period { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("secret")]
            public sbyte[] Secret { get; set; }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                var type = Type switch
                {
                    "TOTP" => AuthenticatorType.Totp,
                    "HOTP" => AuthenticatorType.Hotp,
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
                    Issuer = issuer.Truncate(Authenticator.IssuerMaxLength),
                    Username = username.Truncate(Authenticator.UsernameMaxLength),
                    Algorithm = algorithm,
                    Type = type,
                    Counter = Counter,
                    Digits = Digits,
                    Icon = iconResolver.FindServiceKeyByName(issuer),
                    Period = Period,
                    Secret = Base32.Rfc4648.Encode((ReadOnlySpan<byte>) (Array) Secret)
                };
            }
        }
    }
}