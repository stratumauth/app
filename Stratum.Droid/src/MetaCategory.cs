// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace Stratum.Droid
{
    public static class MetaCategory
    {
        public const string All = "ALL";

        public const string Uncategorised = "UNCATEGORISED";

        public static bool Is(string id)
        {
            return id is All or Uncategorised;
        }
    }
}
