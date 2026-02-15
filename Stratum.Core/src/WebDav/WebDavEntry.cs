// Copyright (C) 2024 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;

namespace Stratum.Core.WebDav
{
    public class WebDavEntry
    {
        public string Name { get; set; }
        public string Href { get; set; }
        public DateTime LastModified { get; set; }
        public long ContentLength { get; set; }
        public bool IsDirectory { get; set; }
    }
}
