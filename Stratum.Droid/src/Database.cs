// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Stratum.Core.Entity;
using Serilog;
using SQLite;
using Stratum.Core;

namespace Stratum.Droid
{
    public class Database
    {
        public enum Origin
        {
            Application,
            Activity,
            Wear,
            AutoBackup,
            Other
        }

        private const string FileName = "authenticator.db3";
        private const SQLiteOpenFlags Flags = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex;

        private readonly ILogger _log = Log.ForContext<Database>();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly ReferenceCounted<SQLiteAsyncConnection> _connection = new(connection => connection.CloseAsync());

        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            await _lock.WaitAsync();

            try
            {
                if (_connection.Value == null)
                {
                    throw new InvalidOperationException("Connection not open");
                }

                return _connection.Value;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> IsOpenAsync()
        {
            await _lock.WaitAsync();

            try
            {
                return _connection.Value != null;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task AcquireAsync(string password, Origin origin)
        {
            await _lock.WaitAsync();

            try
            {
                if (password == "")
                {
                    password = null;
                }

                var path = GetPath();
                var firstLaunch = !File.Exists(path);

                if (!firstLaunch)
                {
                    await VerifyPasswordAsync(path, password);
                }

                await _connection.AcquireAsync(() =>
                {
                    _log.Debug("Opening database from {Origin}", origin);
                    return OpenConnectionAsync(path, password, firstLaunch);
                });
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task ReleaseAsync(Origin origin)
        {
            await _lock.WaitAsync();

            try
            {
                if (await _connection.ReleaseAsync())
                {
                    _log.Debug("Closed database from {Origin}", origin);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<T> UseAsync<T>(string password, Origin origin, Func<Task<T>> action)
        {
            await AcquireAsync(password, origin);

            try
            {
                return await action();
            }
            finally
            {
                await ReleaseAsync(origin);
            }
        }

        private async Task ReopenAsync(string password, Origin origin)
        {
            await _lock.WaitAsync();

            try
            {
                _log.Debug("Reopening database from {Origin}", origin);

                if (password == "")
                {
                    password = null;
                }

                await _connection.ReplaceAsync(async () =>
                {
                    var path = GetPath();
                    await VerifyPasswordAsync(path, password);
                    return await OpenConnectionAsync(path, password, false);
                });
            }
            finally
            {
                _lock.Release();
            }
        }

        private static Task VerifyPasswordAsync(string path, string password)
        {
            return Task.Run(() =>
            {
                var connStr = new SQLiteConnectionString(path, SQLiteOpenFlags.ReadWrite, true, password);
                using var connection = new SQLiteConnection(connStr);
                connection.ExecuteScalar<int>("SELECT count(*) FROM sqlite_master");
            });
        }

        private async Task<SQLiteAsyncConnection> OpenConnectionAsync(string path, string password, bool firstLaunch)
        {
            var connStr = new SQLiteConnectionString(path, Flags, true, password);
            var connection = new SQLiteAsyncConnection(connStr);

            try
            {
                await MigrateAsync(connection, firstLaunch);
            }
            catch
            {
                await connection.CloseAsync();
                throw;
            }

#if DEBUG
            connection.Trace = true;
            connection.Tracer = _log.Debug;
            connection.TimeExecution = true;
#endif

            return connection;
        }

        private static async Task MigrateAsync(SQLiteAsyncConnection connection, bool firstLaunch)
        {
            if (firstLaunch)
            {
                await connection.EnableWriteAheadLoggingAsync();
            }

            await connection.CreateTableAsync<Authenticator>();
            await connection.CreateTableAsync<Category>();
            await connection.CreateTableAsync<AuthenticatorCategory>();
            await connection.CreateTableAsync<CustomIcon>();
            await connection.CreateTableAsync<IconPack>();
            await connection.CreateTableAsync<IconPackEntry>();
        }

        private static string GetPath()
        {
            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var databasesDir = Path.Combine(Directory.GetParent(appDataDir).FullName, "databases");
            Directory.CreateDirectory(databasesDir);
            return Path.Combine(databasesDir, FileName);
        }

        public async Task SetPasswordAsync(string currentPassword, string newPassword)
        {
            if (currentPassword == newPassword)
            {
                return;
            }

            var dbPath = GetPath();
            var backupPath = dbPath + ".backup";

            void DeleteDatabase()
            {
                File.Delete(dbPath);
                File.Delete(dbPath.Replace("db3", "db3-shm"));
                File.Delete(dbPath.Replace("db3", "db3-wal"));
            }

            void RestoreBackup()
            {
                DeleteDatabase();
                File.Move(backupPath, dbPath);
            }

            var conn = await GetConnectionAsync();
            await conn.ExecuteScalarAsync<string>("PRAGMA wal_checkpoint(TRUNCATE)");

            File.Copy(dbPath, backupPath, true);

            // Change encryption mode
            if (currentPassword == null || newPassword == null)
            {
                var tempPath = dbPath + ".temp";

                try
                {
                    if (newPassword != null)
                    {
                        await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ?", tempPath, newPassword);
                    }
                    else
                    {
                        await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ''", tempPath);
                    }

                    await conn.ExecuteScalarAsync<string>("SELECT sqlcipher_export('temporary')");
                }
                catch
                {
                    File.Delete(tempPath);
                    File.Delete(backupPath);
                    throw;
                }
                finally
                {
                    await conn.ExecuteAsync("DETACH DATABASE temporary");
                }

                try
                {
                    await CloseConnectionAsync(Origin.Other);
                    DeleteDatabase();
                    File.Move(tempPath, dbPath);
                    await ReopenAsync(newPassword, Origin.Other);
                }
                catch
                {
                    // Perhaps it wasn't moved correctly
                    File.Delete(tempPath);
                    RestoreBackup();
                    await ReopenAsync(currentPassword, Origin.Other);
                    throw;
                }
                finally
                {
                    File.Delete(backupPath);
                }
            }
            // Change password
            else
            {
                // Cannot use parameters with pragma https://github.com/ericsink/SQLitePCL.raw/issues/153
                var quoted = "'" + newPassword.Replace("'", "''") + "'";

                try
                {
                    await conn.ExecuteScalarAsync<string>($"PRAGMA rekey = {quoted}");

                    await CloseConnectionAsync(Origin.Other);
                    await ReopenAsync(newPassword, Origin.Other);
                }
                catch
                {
                    RestoreBackup();
                    await ReopenAsync(currentPassword, Origin.Other);
                    throw;
                }
                finally
                {
                    File.Delete(backupPath);
                }
            }
        }

        private async Task CloseConnectionAsync(Origin origin)
        {
            await _lock.WaitAsync();

            try
            {
                if (_connection.Value == null)
                {
                    return;
                }

                _log.Debug("Closing database connection from {Origin}", origin);
                await _connection.DestroyAsync();
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
