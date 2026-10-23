// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stratum.Droid
{
    public class DatabaseSession
    {
        private readonly Database _database;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private volatile bool _isUnlocked;

        public DatabaseSession(Database database)
        {
            _database = database;
        }

        public bool IsUnlocked => _isUnlocked;

        public async Task UnlockAsync(string password, Database.Origin origin)
        {
            await _lock.WaitAsync();

            try
            {
                if (_isUnlocked)
                {
                    return;
                }

                await _database.AcquireAsync(password, origin);
                _isUnlocked = true;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task LockAsync(Database.Origin origin)
        {
            await _lock.WaitAsync();

            try
            {
                if (!_isUnlocked)
                {
                    return;
                }

                _isUnlocked = false;
                await _database.ReleaseAsync(origin);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SetPasswordAsync(string currentPassword, string newPassword)
        {
            await _lock.WaitAsync();

            try
            {
                if (!_isUnlocked)
                {
                    throw new InvalidOperationException("Database is locked");
                }

                try
                {
                    await _database.SetPasswordAsync(currentPassword, newPassword);
                }
                catch
                {
                    if (!await _database.IsOpenAsync())
                    {
                        _isUnlocked = false;
                        await _database.ReleaseAsync(Database.Origin.Other);
                    }

                    throw;
                }
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
