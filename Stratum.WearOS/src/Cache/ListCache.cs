// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;

namespace Stratum.WearOS.Cache
{
    public class ListCache<T> : IEnumerable, IDisposable
    {
        private readonly string _name;
        private readonly Context _context;
        private readonly SemaphoreSlim _flushLock;
        private List<T> _items;
        private bool _isDisposed;

        public ListCache(string name, Context context)
        {
            _name = name;
            _context = context;
            _items = [];
            _flushLock = new SemaphoreSlim(1, 1);
        }

        public int Count => _items.Count;

        public T this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        ~ListCache()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _flushLock.Dispose();
            }

            _isDisposed = true;
        }

        private string GetFilePath()
        {
            return $"{_context.CacheDir}/{_name}.json";
        }

        public async Task InitAsync()
        {
            var path = GetFilePath();

            if (!File.Exists(path))
            {
                return;
            }

            await using var stream = File.OpenRead(path);
            _items = await JsonSerializer.DeserializeAsync<List<T>>(stream);
        }

        public Task ReplaceAsync(List<T> items)
        {
            _items = items;
            return FlushAsync();
        }

        public bool Dirty(IEnumerable<T> items, IEqualityComparer<T> comparer = null)
        {
            return comparer != null
                ? !_items.SequenceEqual(items, comparer)
                : !_items.SequenceEqual(items);
        }

        private async Task FlushAsync()
        {
            await _flushLock.WaitAsync();

            try
            {
                await using var stream = File.Open(GetFilePath(), FileMode.Create);
                await JsonSerializer.SerializeAsync(stream, _items);
            }
            finally
            {
                _flushLock.Release();
            }
        }

        public List<T> GetItems()
        {
            return _items;
        }

        private IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public int FindIndex(Predicate<T> predicate)
        {
            return _items.FindIndex(predicate);
        }
    }
}