// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Stratum.Core;
using Xunit;

namespace Stratum.Test.General
{
    public class ReferenceCountedTest
    {
        [Fact]
        public async Task AcquireAsync_creates()
        {
            var counted = new ReferenceCounted<object>(_ => Task.CompletedTask);
            var value = new object();

            await counted.AcquireAsync(() => Task.FromResult(value));

            Assert.Same(value, counted.Value);
        }

        [Fact]
        public async Task AcquireAsync_existing()
        {
            var counted = new ReferenceCounted<object>(_ => Task.CompletedTask);
            var value = new object();

            await counted.AcquireAsync(() => Task.FromResult(value));
            await counted.AcquireAsync(() => throw new InvalidOperationException());

            Assert.Same(value, counted.Value);
        }

        [Fact]
        public async Task AcquireAsync_createThrows()
        {
            var counted = new ReferenceCounted<object>(_ => Task.CompletedTask);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                counted.AcquireAsync(() => throw new InvalidOperationException()));

            Assert.Null(counted.Value);
            await Assert.ThrowsAsync<InvalidOperationException>(counted.ReleaseAsync);
        }

        [Fact]
        public async Task AcquireAsync_afterRelease()
        {
            var counted = new ReferenceCounted<object>(_ => Task.CompletedTask);
            var value = new object();

            await counted.AcquireAsync(() => Task.FromResult(new object()));
            await counted.ReleaseAsync();
            await counted.AcquireAsync(() => Task.FromResult(value));

            Assert.Same(value, counted.Value);
        }

        [Fact]
        public async Task ReleaseAsync_none()
        {
            var counted = new ReferenceCounted<object>(_ => Task.CompletedTask);
            await Assert.ThrowsAsync<InvalidOperationException>(counted.ReleaseAsync);
        }

        [Fact]
        public async Task ReleaseAsync_last()
        {
            object destroyed = null;

            var counted = new ReferenceCounted<object>(value =>
            {
                destroyed = value;
                return Task.CompletedTask;
            });

            var value = new object();
            await counted.AcquireAsync(() => Task.FromResult(value));

            Assert.True(await counted.ReleaseAsync());
            Assert.Null(counted.Value);
            Assert.Same(value, destroyed);
        }

        [Fact]
        public async Task ReleaseAsync_remaining()
        {
            object destroyed = null;

            var counted = new ReferenceCounted<object>(value =>
            {
                destroyed = value;
                return Task.CompletedTask;
            });

            var value = new object();
            await counted.AcquireAsync(() => Task.FromResult(value));
            await counted.AcquireAsync(() => throw new InvalidOperationException());

            Assert.False(await counted.ReleaseAsync());
            Assert.Same(value, counted.Value);
            Assert.Null(destroyed);

            Assert.True(await counted.ReleaseAsync());
            Assert.Null(counted.Value);
            Assert.Same(value, destroyed);
        }

        [Fact]
        public async Task ReleaseAsync_afterDestroy()
        {
            var counted = new ReferenceCounted<object>(_ => Task.CompletedTask);

            await counted.AcquireAsync(() => Task.FromResult(new object()));
            await counted.DestroyAsync();

            Assert.False(await counted.ReleaseAsync());
        }

        [Fact]
        public async Task ReplaceAsync_empty()
        {
            var counted = new ReferenceCounted<object>(_ => Task.CompletedTask);
            var value = new object();

            await counted.ReplaceAsync(() => Task.FromResult(value));

            Assert.Same(value, counted.Value);
        }

        [Fact]
        public async Task ReplaceAsync_existing()
        {
            object destroyed = null;

            var counted = new ReferenceCounted<object>(value =>
            {
                destroyed = value;
                return Task.CompletedTask;
            });

            var first = new object();
            var second = new object();

            await counted.AcquireAsync(() => Task.FromResult(first));
            await counted.ReplaceAsync(() => Task.FromResult(second));

            Assert.Same(second, counted.Value);
            Assert.Same(first, destroyed);
        }

        [Fact]
        public async Task ReplaceAsync_keepsCount()
        {
            object destroyed = null;

            var counted = new ReferenceCounted<object>(value =>
            {
                destroyed = value;
                return Task.CompletedTask;
            });

            var replacement = new object();

            await counted.AcquireAsync(() => Task.FromResult(new object()));
            await counted.AcquireAsync(() => throw new InvalidOperationException());
            await counted.ReplaceAsync(() => Task.FromResult(replacement));

            Assert.False(await counted.ReleaseAsync());
            Assert.Same(replacement, counted.Value);

            Assert.True(await counted.ReleaseAsync());
            Assert.Same(replacement, destroyed);
        }

        [Fact]
        public async Task ReplaceAsync_createThrows()
        {
            object destroyed = null;

            var counted = new ReferenceCounted<object>(value =>
            {
                destroyed = value;
                return Task.CompletedTask;
            });

            var value = new object();
            await counted.AcquireAsync(() => Task.FromResult(value));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                counted.ReplaceAsync(() => throw new InvalidOperationException()));

            Assert.Null(counted.Value);
            Assert.Same(value, destroyed);
            Assert.False(await counted.ReleaseAsync());
        }

        [Fact]
        public async Task DestroyAsync_empty()
        {
            object destroyed = null;

            var counted = new ReferenceCounted<object>(value =>
            {
                destroyed = value;
                return Task.CompletedTask;
            });

            await counted.DestroyAsync();

            Assert.Null(destroyed);
        }

        [Fact]
        public async Task DestroyAsync_clearsValueFirst()
        {
            ReferenceCounted<object> counted = null;
            var wasCleared = false;

            counted = new ReferenceCounted<object>(_ =>
            {
                wasCleared = counted.Value == null;
                return Task.CompletedTask;
            });

            await counted.AcquireAsync(() => Task.FromResult(new object()));
            await counted.DestroyAsync();

            Assert.True(wasCleared);
        }
    }
}
