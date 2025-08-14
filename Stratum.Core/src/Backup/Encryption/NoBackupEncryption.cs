// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Stratum.Core.Backup.Encryption
{
    public class NoBackupEncryption : IBackupEncryption
    {
        public Task<byte[]> EncryptAsync(Backup backup, string password)
        {
            return Task.FromResult(JsonSerializer.SerializeToUtf8Bytes(backup));
        }

        public Task<Backup> DecryptAsync(byte[] data, string password)
        {
            return Task.FromResult(JsonSerializer.Deserialize<Backup>(data));
        }

        public bool CanBeDecrypted(byte[] data)
        {
            Backup backup;

            try
            {
                backup = JsonSerializer.Deserialize<Backup>(data);
            }
            catch (Exception)
            {
                return false;
            }

            return backup?.Authenticators != null;
        }
    }
}