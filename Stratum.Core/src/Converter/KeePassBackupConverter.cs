// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Konscious.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Stratum.Core.Backup;

namespace Stratum.Core.Converter
{
    public class KeePassBackupConverter : UriListBackupConverter
    {
        // Header
        private const uint HeaderSignature1 = 0x9AA2D903;
        private const uint HeaderSignature2 = 0xB54BFB67;
        private static readonly byte[] EndOfHeader = [0xD, 0xA, 0xD, 0xA];

        // Encryption
        private static readonly byte[] AesUuid =
            [0x31, 0xC1, 0xF2, 0xE6, 0xBF, 0x71, 0x43, 0x50, 0xBE, 0x58, 0x05, 0x21, 0x6A, 0xFC, 0x5A, 0xFF];

        private static readonly byte[] ChaChaUuid =
            [0xD6, 0x03, 0x8A, 0x2B, 0x8B, 0x6F, 0x4C, 0xB5, 0xA5, 0x24, 0x33, 0x9A, 0x31, 0xDB, 0xB5, 0x9A];

        private static readonly byte[] TwoFishUuid =
            [0xAD, 0x68, 0xF2, 0x9F, 0x57, 0x6F, 0x4B, 0xB9, 0xA3, 0x6A, 0xD4, 0x7A, 0xF9, 0x65, 0x34, 0x6C];

        private const string AesAlgorithmDescription = "AES/CBC/PKCS7";
        private const string AesKdfAlgorithmDescription = "AES/ECB/NoPadding";
        private const string ChaChaAlgorithmDescription = "ChaCha20";
        private const string SalsaAlgorithmDescription = "Salsa20";
        private const string TwoFishAlgorithmDescription = "Twofish/CBC/NoPadding";

        // Key derivation
        private static readonly byte[] AesKdfUuid =
            [0xC9, 0xD9, 0xF3, 0x9A, 0x62, 0x8A, 0x44, 0x60, 0xBF, 0x74, 0x0D, 0x08, 0xC1, 0x8A, 0x4F, 0xEA];

        private static readonly byte[] Argon2dUuid =
            [0xEF, 0x63, 0x6D, 0xDF, 0x8C, 0x29, 0x44, 0x4B, 0x91, 0xF7, 0xA9, 0xA4, 0x03, 0xE3, 0x0A, 0x0C];

        private static readonly byte[] Argon2idUuid =
            [0x9E, 0x29, 0x8B, 0x19, 0x56, 0xDB, 0x47, 0x73, 0xB2, 0x3D, 0xFC, 0x3E, 0xC6, 0xF0, 0xA1, 0xE6];

        // Lengths
        private const int KeyLength = 32;
        private const int HeaderHashLength = 32;

        // Constants
        private static readonly byte[] AuthenticityHashSeed = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        private static readonly byte[] SalsaIv = [0xE8, 0x30, 0x09, 0x4B, 0x97, 0x20, 0x5D, 0x2A];

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Always;

        public KeePassBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override async Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);
            var header = ReadHeader(reader);

            var transformedKey = await DeriveKeyAsync(header.KdfParameters, password);
            VerifyHeader(header, transformedKey);

            var masterKey = GetMasterKey(header.MasterSalt, transformedKey);

            var uris = await Task.Run(() =>
            {
                using var blockStream = GetBlockStream(reader, header, masterKey, transformedKey);
                var database = ReadDatabase(blockStream);
                return GetUris(database);
            });

            return ConvertLines(uris);
        }

        private static IBufferedCipher GetInnerDecryptionCipher(InnerEncryptionAlgorithm algorithm, byte[] innerKey)
        {
            byte[] key;
            byte[] iv;
            string cipherDescription;

            switch (algorithm)
            {
                case InnerEncryptionAlgorithm.Salsa20:
                    key = SHA256.HashData(innerKey);
                    iv = SalsaIv;
                    cipherDescription = SalsaAlgorithmDescription;
                    break;

                case InnerEncryptionAlgorithm.ChaCha20:
                {
                    var hash = SHA512.HashData(innerKey);
                    key = hash.Take(32).ToArray();
                    iv = hash.Skip(32).Take(12).ToArray();
                    cipherDescription = ChaChaAlgorithmDescription;
                    break;
                }

                default:
                    throw new ArgumentException($"Unsupported inner encryption algorithm {algorithm}");
            }

            var keyParameter = new ParametersWithIV(new KeyParameter(key), iv);
            var cipher = CipherUtilities.GetCipher(cipherDescription);
            cipher.Init(false, keyParameter);

            return cipher;
        }

        private static List<string> GetUris(KeepassDatabase keepassDatabase)
        {
            var cipher = GetInnerDecryptionCipher(
                keepassDatabase.InnerEncryptionAlgorithm,
                keepassDatabase.InnerEncryptionKey);

            var recycleBinUuid = keepassDatabase.Document.SelectSingleNode("//RecycleBinUUID")?.InnerText;
            var uris = new List<string>();

            foreach (XmlNode valueNode in keepassDatabase.Document.SelectNodes("//Value"))
            {
                string value;

                if (valueNode.Attributes != null &&
                    valueNode.Attributes.GetNamedItem("Protected")?.Value == "True")
                {
                    // Since this is a stream cipher we need to decrypt every protected field (even if unused)
                    var valueBytes = Convert.FromBase64String(valueNode.InnerText);
                    var decryptedBytes = cipher.ProcessBytes(valueBytes) ?? cipher.DoFinal(valueBytes);
                    value = Encoding.UTF8.GetString(decryptedBytes);
                }
                else
                {
                    value = valueNode.InnerText;
                }
                
                var itemNode = valueNode.ParentNode;

                if (itemNode.Name != "String")
                {
                    continue;
                }
                
                var keyNode = itemNode.SelectSingleNode("Key");
                var entryNode = itemNode.ParentNode;
                var groupNode = entryNode.ParentNode;
                
                var groupId = groupNode.SelectSingleNode("UUID")?.InnerText;

                if (keyNode.InnerText == "otp" && groupNode.Name != "History" &&
                    groupId != recycleBinUuid)
                {
                    uris.Add(value);
                }
            }

            return uris;
        }

        private static KeepassDatabase ReadDatabase(Stream stream)
        {
            var binaryReader = new BinaryReader(stream);
            var database = new KeepassDatabase();

            while (true)
            {
                var id = binaryReader.ReadByte();
                var size = binaryReader.ReadInt32();

                if (id == 0)
                {
                    break;
                }

                switch (id)
                {
                    case 1:
                        database.InnerEncryptionAlgorithm = (InnerEncryptionAlgorithm) binaryReader.ReadInt32();
                        break;

                    case 2:
                        database.InnerEncryptionKey = binaryReader.ReadBytes(size);
                        break;

                    case 3:
                        // Binary content, skip
                        binaryReader.ReadBytes(size);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown inner header field {id}");
                }
            }

            database.Document = new XmlDocument();
            database.Document.Load(stream);

            return database;
        }

        private static Stream GetBlockStream(BinaryReader reader, Header header, 
            byte[] masterKey, byte[] transformedKey)
        {
            var cipherDescription = header.EncryptionAlgorithm switch
            {
                EncryptionAlgorithm.Aes256 => AesAlgorithmDescription,
                EncryptionAlgorithm.ChaCha20 => ChaChaAlgorithmDescription,
                EncryptionAlgorithm.TwoFish => TwoFishAlgorithmDescription,
                _ => throw new ArgumentException($"Unsupported encryption algorithm {header.EncryptionAlgorithm}")
            };

            var keyParameter = new ParametersWithIV(new KeyParameter(masterKey), header.EncryptionIv);
            var cipher = CipherUtilities.GetCipher(cipherDescription);
            cipher.Init(false, keyParameter);

            var decryptedStream = new MemoryStream();

            foreach (var blockData in ReadRawBlocks(reader, header.MasterSalt, transformedKey))
            {
                if (blockData.Length == 0)
                {
                    decryptedStream.Write(cipher.DoFinal());
                    break;
                }

                var decrypted = cipher.ProcessBytes(blockData);
                decryptedStream.Write(decrypted);
            }

            decryptedStream.Seek(0, SeekOrigin.Begin);

            return header.CompressionAlgorithm switch
            {
                CompressionAlgorithm.None => decryptedStream,
                CompressionAlgorithm.GZip => new GZipStream(decryptedStream, CompressionMode.Decompress),
                _ => throw new ArgumentException($"Unsupported compression algorithm {header.CompressionAlgorithm}")
            };
        }

        private static HMACSHA256 GetBlockHmac(ulong blockId, byte[] masterSalt, byte[] transformedKey)
        {
            var initialAuthHashMaterial = Concat(masterSalt, transformedKey, [1]);
            var initialAuthHash = SHA512.HashData(initialAuthHashMaterial);

            var keyHashMaterial = Concat(BitConverter.GetBytes(blockId), initialAuthHash);
            var key = SHA512.HashData(keyHashMaterial);

            var hmac = new HMACSHA256();
            hmac.Key = key;

            return hmac;
        }

        private static IEnumerable<byte[]> ReadRawBlocks(BinaryReader reader, byte[] masterSalt, byte[] transformedKey)
        {
            ulong blockId = 0;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var expectedAuthHash = reader.ReadBytes(32);
                var blockSizeBytes = reader.ReadBytes(4);
                var block = reader.ReadBytes(BitConverter.ToInt32(blockSizeBytes));

                using var hmac = GetBlockHmac(blockId, masterSalt, transformedKey);
                var authHashMaterial = Concat(BitConverter.GetBytes(blockId), blockSizeBytes, block);
                var authHash = hmac.ComputeHash(authHashMaterial);

                if (!Arrays.ConstantTimeAreEqual(authHash, expectedAuthHash))
                {
                    throw new InvalidOperationException($"Block {blockId} MAC does not match");
                }

                yield return block;
                blockId++;
            }
        }

        private static void VerifyHeader(Header header, byte[] transformedKey)
        {
            var integrityHash = SHA256.HashData(header.Bytes);

            if (!integrityHash.SequenceEqual(header.IntegrityHash))
            {
                throw new ArgumentException("Header integrity check failed");
            }

            var initialAuthHashMaterial = Concat(header.MasterSalt, transformedKey, [1]);
            var initialAuthHash = SHA512.HashData(initialAuthHashMaterial);

            var authKeyMaterial = Concat(AuthenticityHashSeed, initialAuthHash);
            var authKey = SHA512.HashData(authKeyMaterial);

            using var hmacSha256 = new HMACSHA256();
            hmacSha256.Key = authKey;

            var authHash = hmacSha256.ComputeHash(header.Bytes);

            if (!Arrays.ConstantTimeAreEqual(authHash, header.AuthenticityHash))
            {
                throw new BackupPasswordException("Header authenticity check failed");
            }
        }

        private static byte[] Concat(params byte[][] arrays)
        {
            var output = new byte[arrays.Sum(a => a.Length)];
            var offset = 0;

            foreach (var array in arrays)
            {
                Buffer.BlockCopy(array, 0, output, offset, array.Length);
                offset += array.Length;
            }

            return output;
        }

        private static async Task<byte[]> DeriveKeyAsync(VariantDictionary kdfParameters, string password)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            // We would normally concatenate other components such as a key file
            // Only a password is supported for this converter
            var compositeKey = SHA256.HashData(SHA256.HashData(passwordBytes));

            var kdfUuid = kdfParameters.GetBytes("$UUID");

            if (kdfUuid.SequenceEqual(AesKdfUuid))
            {
                var salt = kdfParameters.GetBytes("S");
                var rounds = kdfParameters.GetUInt64("R");
                var transformedKey = compositeKey;

                var cipher = CipherUtilities.GetCipher(AesKdfAlgorithmDescription);
                cipher.Init(true, new KeyParameter(salt));

                for (ulong i = 0; i < rounds; ++i)
                {
                    transformedKey = cipher.ProcessBytes(transformedKey);
                }

                return SHA256.HashData(transformedKey);
            }
            else
            {
                var isArgon2d = kdfUuid.SequenceEqual(Argon2dUuid);
                var isArgon2Id = !isArgon2d && kdfUuid.SequenceEqual(Argon2idUuid);

                if (!isArgon2d && !isArgon2Id)
                {
                    throw new ArgumentException("Unknown key derivation function");
                }

                var salt = kdfParameters.GetBytes("S");
                var iterations = kdfParameters.GetUInt64("I");
                var memory = kdfParameters.GetUInt64("M");
                var parallelism = kdfParameters.GetUInt32("P");

                Argon2 argon2 = isArgon2d ? new Argon2d(compositeKey) : new Argon2id(compositeKey);
                argon2.DegreeOfParallelism = (int) parallelism;
                argon2.Iterations = (int) iterations;
                argon2.MemorySize = (int) (memory / 1024);
                argon2.Salt = salt;

                return await argon2.GetBytesAsync(KeyLength);
            }
        }

        private static byte[] GetMasterKey(byte[] salt, byte[] transformedKey)
        {
            return SHA256.HashData(Concat(salt, transformedKey));
        }

        private static Header ReadHeader(BinaryReader reader)
        {
            var signature1 = reader.ReadUInt32();
            var signature2 = reader.ReadUInt32();

            if (signature1 != HeaderSignature1 || signature2 != HeaderSignature2)
            {
                throw new ArgumentException("Not a KeePass file");
            }

            var header = new Header
            {
                MinorVersion = reader.ReadUInt16(),
                MajorVersion = reader.ReadUInt16()
            };

            if (header.MajorVersion != 4)
            {
                throw new ArgumentException("Only version 4 KDBX files are supported");
            }

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var id = reader.ReadByte();

                var messageSize = reader.ReadInt32();
                var message = reader.ReadBytes(messageSize);

                if (id == 0)
                {
                    if (!message.SequenceEqual(EndOfHeader))
                    {
                        throw new ArgumentException("Expected end of header sequence");
                    }

                    var headerEnd = (int) reader.BaseStream.Position;
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    header.Bytes = reader.ReadBytes(headerEnd);

                    header.IntegrityHash = reader.ReadBytes(HeaderHashLength);
                    header.AuthenticityHash = reader.ReadBytes(HeaderHashLength);

                    return header;
                }

                switch (id)
                {
                    case 2 when message.SequenceEqual(AesUuid):
                        header.EncryptionAlgorithm = EncryptionAlgorithm.Aes256;
                        break;

                    case 2 when message.SequenceEqual(ChaChaUuid):
                        header.EncryptionAlgorithm = EncryptionAlgorithm.ChaCha20;
                        break;

                    case 2 when message.SequenceEqual(TwoFishUuid):
                        header.EncryptionAlgorithm = EncryptionAlgorithm.TwoFish;
                        break;

                    case 2:
                        throw new ArgumentException("Unknown encryption algorithm");

                    case 3:
                        header.CompressionAlgorithm = (CompressionAlgorithm) message[0];
                        break;

                    case 4:
                        header.MasterSalt = message;
                        break;

                    case 7:
                        header.EncryptionIv = message;
                        break;

                    case 11:
                        header.KdfParameters = ReadVariantDictionary(message);
                        break;
                }
            }

            throw new InvalidOperationException("Incomplete header");
        }

        private static VariantDictionary ReadVariantDictionary(byte[] message)
        {
            using var memoryStream = new MemoryStream(message);
            using var reader = new BinaryReader(memoryStream);

            var minorVersion = reader.ReadByte();
            var majorVersion = reader.ReadByte();

            if (majorVersion > 1 || minorVersion > 0)
            {
                throw new ArgumentException($"Only version 1.0 variant dictionaries are supported. Got version {majorVersion}.{minorVersion}");
            }

            var dictionary = new VariantDictionary();

            while (memoryStream.Position < memoryStream.Length)
            {
                var type = reader.ReadByte();

                if (type == '\0')
                {
                    break;
                }

                var nameLength = reader.ReadInt32();
                var name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
                var valueLength = reader.ReadInt32();

                switch (type)
                {
                    case 0x04:
                        dictionary.Add(name, reader.ReadUInt32());
                        break;

                    case 0x05:
                        dictionary.Add(name, reader.ReadUInt64());
                        break;

                    case 0x08:
                        dictionary.Add(name, reader.ReadBoolean());
                        break;

                    case 0x0C:
                        dictionary.Add(name, reader.ReadInt32());
                        break;

                    case 0x0D:
                        dictionary.Add(name, reader.ReadInt64());
                        break;

                    case 0x18:
                        dictionary.Add(name, Encoding.UTF8.GetString(reader.ReadBytes(valueLength)));
                        break;

                    case 0x42:
                        dictionary.Add(name, reader.ReadBytes(valueLength));
                        break;
                }
            }

            return dictionary;
        }

        private enum EncryptionAlgorithm
        {
            Aes256 = 0,
            ChaCha20 = 1,
            TwoFish = 2
        }

        private enum InnerEncryptionAlgorithm
        {
            Salsa20 = 2,
            ChaCha20 = 3
        }

        private enum CompressionAlgorithm
        {
            None = 0,
            GZip = 1
        }

        private sealed class Header
        {
            public ushort MajorVersion { get; set; }
            public ushort MinorVersion { get; set; }
            public EncryptionAlgorithm EncryptionAlgorithm { get; set; }
            public CompressionAlgorithm CompressionAlgorithm { get; set; }
            public byte[] MasterSalt { get; set; }
            public byte[] EncryptionIv { get; set; }
            public VariantDictionary KdfParameters { get; set; }
            public byte[] IntegrityHash { get; set; }
            public byte[] AuthenticityHash { get; set; }
            public byte[] Bytes { get; set; }
        }

        private sealed class VariantDictionary : Dictionary<string, object>
        {
            public byte[] GetBytes(string name)
            {
                return (byte[]) this[name];
            }

            public ulong GetUInt64(string name)
            {
                return (ulong) this[name];
            }

            public uint GetUInt32(string name)
            {
                return (uint) this[name];
            }
        }

        private sealed class KeepassDatabase
        {
            public InnerEncryptionAlgorithm InnerEncryptionAlgorithm { get; set; }
            public byte[] InnerEncryptionKey { get; set; }
            public XmlDocument Document { get; set; }
        }
    }
}