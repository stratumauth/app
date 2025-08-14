// Copyright (C) 2024 jmh
// SPDX-License-Identifier:GPL-3.0-only

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Stratum.Core.Backup;
using Stratum.Core.Converter.Crypto;
using Argon2id = Konscious.Security.Cryptography.Argon2id;

namespace Stratum.Core.Converter
{
    public class EnteAuthBackupConverter : UriListBackupConverter
    {
        private const int KeyLength = 32;
        private const int DerivationParallelism = 1;
        
        public EnteAuthBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Maybe;
        
        public override async Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            if (password == null)
            {
                return await base.ConvertAsync(data);
            }
            
            var backup = JsonSerializer.Deserialize<EnteBackup>(data);

            if (backup.Version != 1)
            {
                throw new NotSupportedException("Unsupported backup version");
            }

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var key = await DeriveKeyAsync(passwordBytes, backup.DerivationParams);
            
            var encryptedData = Convert.FromBase64String(backup.EncryptedData);
            var header = Convert.FromBase64String(backup.EncryptionNonce);
            
            var decryptedData = await Task.Run(() => Decrypt(key, encryptedData, header));
            return await base.ConvertAsync(decryptedData);
        }
        
        private static async Task<byte[]> DeriveKeyAsync(byte[] password, KdfParams parameters)
        {
            var argon2 = new Argon2id(password);
            argon2.DegreeOfParallelism = DerivationParallelism;
            argon2.Iterations = parameters.OperationsLimit;
            argon2.MemorySize = (int) (parameters.MemoryLimit / 1024);
            argon2.Salt = Convert.FromBase64String(parameters.Salt);

            return await argon2.GetBytesAsync(KeyLength);
        }

        private static byte[] Decrypt(byte[] key, byte[] data, byte[] header)
        {
            var stream = new XChaCha20Poly1305Stream();
            stream.Init(key, header);
            
            XChaCha20Poly1305Stream.Message message;
            
            try
            {
                message = stream.Pull(data);
            }
            catch (InvalidCipherTextException e)
            {
                throw new BackupPasswordException("The password is incorrect", e);
            }
            
            // Don't check the tag, there is only one message
            return message.Data;
        }
        
        private sealed class EnteBackup
        {
            [JsonPropertyName("version")]
            public int Version { get; set; }
            
            [JsonPropertyName("kdfParams")]
            public KdfParams DerivationParams { get; set; }
            
            [JsonPropertyName("encryptedData")]
            public string EncryptedData { get; set; }
            
            [JsonPropertyName("encryptionNonce")]
            public string EncryptionNonce { get; set; }
        }

        private sealed class KdfParams
        {
            [JsonPropertyName("memLimit")]
            public long MemoryLimit { get; set; } 
            
            [JsonPropertyName("opsLimit")]
            public int OperationsLimit { get; set; } 
            
            [JsonPropertyName("salt")]
            public string Salt { get; set; } 
        }
    }
}