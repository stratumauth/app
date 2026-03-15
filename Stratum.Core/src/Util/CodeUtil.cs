// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;

namespace Stratum.Core.Util
{
    public static class CodeUtil
    {
        public static string PadCode(string code, int digits, CodeGrouping grouping)
        {
            var filledCode = code ?? new string('–', digits);

            var groupSize = grouping switch
            {
                CodeGrouping.Two => 2,
                CodeGrouping.Three => 3,
                CodeGrouping.Four => 4,
                _ => 0
            };

            if (groupSize > 0)
            {
                return PadByGroupSize(filledCode, digits, groupSize);
            }

            var split = grouping switch
            {
                CodeGrouping.Halves => 2,
                CodeGrouping.Thirds => 3,
                _ => 0
            };

            return PadFractionally(filledCode, split);
        }

        private static string PadByGroupSize(string code, int digits, int groupSize)
        {
            var padded = code;
            var spacesInserted = 0;

            for (var i = 0; i < digits; ++i)
            {
                if (i % groupSize == 0 && i > 0)
                {
                    padded = padded.Insert(i + spacesInserted, " ");
                    spacesInserted++;
                }
            }

            return padded;
        }

        private static string PadFractionally(string code, int division)
        {
            return division switch
            {
                2 => code.Insert((int) Math.Ceiling(code.Length / 2m), " "),
                3 => code.Insert((int) Math.Ceiling(code.Length / 3m), " ")
                    .Insert((int) Math.Ceiling(code.Length / 3m * 2m) + 1, " "),
                _ => code
            };
        }
    }
}