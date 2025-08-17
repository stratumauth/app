// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;

namespace Stratum.Core.Backup.Encryption
{
    public interface IBackupEncryption
    {
        Task<byte[]> EncryptAsync(Backup backup, string password);
        Task<Backup> DecryptAsync(byte[] data, string password);
        bool CanBeDecrypted(byte[] data);
    }
}