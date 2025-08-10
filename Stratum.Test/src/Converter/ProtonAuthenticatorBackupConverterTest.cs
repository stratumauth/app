// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Linq;
using System.Threading.Tasks;
using Stratum.Core;
using Moq;
using Stratum.Core.Converter;
using Stratum.Test.Converter.Fixture;
using Xunit;

namespace Stratum.Test.Converter
{
    public class ProtonAuthenticatorBackupConverterTest : IClassFixture<ProtonAuthenticatorBackupFixture>
    {
        private readonly ProtonAuthenticatorBackupFixture _protonAuthenticatorBackupFixture;
        private readonly ProtonAuthenticatorBackupConverter _protonAuthenticatorBackupConverter;

        public ProtonAuthenticatorBackupConverterTest(ProtonAuthenticatorBackupFixture protonAuthenticatorBackupFixture)
        {
            _protonAuthenticatorBackupFixture = protonAuthenticatorBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _protonAuthenticatorBackupConverter = new ProtonAuthenticatorBackupConverter(iconResolver.Object);
        }

        [Fact]
        public async Task ConvertAsync()
        {
            var result = await _protonAuthenticatorBackupConverter.ConvertAsync(_protonAuthenticatorBackupFixture.Data);

            Assert.Empty(result.Failures);

            Assert.Equal(7, result.Backup.Authenticators.Count());
            Assert.Null(result.Backup.Categories);
            Assert.Null(result.Backup.AuthenticatorCategories);
            Assert.Null(result.Backup.CustomIcons);
        }
    }
}