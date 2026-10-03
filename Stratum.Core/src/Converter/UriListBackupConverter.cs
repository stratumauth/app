// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stratum.Core.Util;
using Stratum.Core.Backup;
using Stratum.Core.Entity;

namespace Stratum.Core.Converter
{
    public class UriListBackupConverter : BackupConverter
    {
        public UriListBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public override Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var text = Encoding.UTF8.GetString(data);
            var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            return Task.FromResult(ConvertLines(lines));
        }

        protected ConversionResult ConvertLines(IEnumerable<string> lines)
        {
            var authenticators = new List<Authenticator>();
            var failures = new List<ConversionFailure>();

            foreach (var line in lines.Select(l => l.Trim()).Where(l => l != string.Empty))
            {
                if (!line.StartsWith("otpauth") && !line.StartsWith("motp"))
                {
                    throw new ArgumentException("Invalid file");
                }
                
                Authenticator auth;

                try
                {
                    auth = UriParser.ParseStandardUri(line, IconResolver).Authenticator;
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure { Description = line, Error = e.Message });
                    continue;
                }

                if (auth.Type.HasPin() && string.IsNullOrEmpty(auth.Pin))
                {
                    failures.Add(new ConversionFailure { Description = line, Error = "Pin required but not provided" });
                    continue;
                }

                auth.Issuer = auth.Issuer.Truncate(Authenticator.IssuerMaxLength);
                auth.Username = auth.Username.Truncate(Authenticator.UsernameMaxLength);

                authenticators.Add(auth);
            }

            var backup = new Backup.Backup { Authenticators = authenticators };
            return new ConversionResult { Failures = failures, Backup = backup };
        }
    }
}