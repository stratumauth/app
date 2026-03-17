// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Stratum.Core;
using Stratum.Core.Util;
using Xunit;

namespace Stratum.Test.Util
{
    public class CodeUtilTest
    {
        [Theory]
        [InlineData(null, "––– –––", 6, CodeGrouping.Three)]
        [InlineData("123456", "123456", 6, CodeGrouping.None)]
        [InlineData("123456", "123456", 0, CodeGrouping.None)]
        [InlineData("123456", "123 456", 6, CodeGrouping.Three)]
        [InlineData("123456789", "123 456 789", 9, CodeGrouping.Three)]
        [InlineData("123456", "12 34 56", 6, CodeGrouping.Two)]
        [InlineData("123456", "1234 56", 6, CodeGrouping.Four)]
        [InlineData("123456", "123 456", 6, CodeGrouping.Halves)]
        [InlineData("123456", "12 34 56", 6, CodeGrouping.Thirds)]
        [InlineData("12345678", "1234 5678", 8, CodeGrouping.Halves)]
        [InlineData("12345678", "123 456 78", 8, CodeGrouping.Thirds)]
        [InlineData("123456789", "12345 6789", 9, CodeGrouping.Halves)]
        [InlineData("123456789", "123 456 789", 9, CodeGrouping.Thirds)]
        [InlineData("12345", "12345", 5, CodeGrouping.Halves)]
        [InlineData("12345", "12345", 5, CodeGrouping.Thirds)]
        public void PadCode(string input, string expected, int digits, CodeGrouping groupSize)
        {
            var padded = CodeUtil.PadCode(input, digits, groupSize);
            Assert.Equal(expected, padded);
        }
    }
}