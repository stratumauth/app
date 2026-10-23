// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;

namespace Stratum.Core
{
    public class ReferenceCounted<T> where T : class
    {
        private readonly Func<T, Task> _destroy;
        private int _count;

        public ReferenceCounted(Func<T, Task> destroy)
        {
            _destroy = destroy;
        }

        public T Value { get; private set; }

        public async Task AcquireAsync(Func<Task<T>> create)
        {
            Value ??= await create();
            _count++;
        }

        public async Task<bool> ReleaseAsync()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("No reference to release");
            }

            if (--_count > 0 || Value == null)
            {
                return false;
            }

            await DestroyAsync();
            return true;
        }

        public async Task ReplaceAsync(Func<Task<T>> create)
        {
            await DestroyAsync();
            Value = await create();
        }

        public async Task DestroyAsync()
        {
            if (Value == null)
            {
                return;
            }

            var value = Value;
            Value = null;
            await _destroy(value);
        }
    }
}
