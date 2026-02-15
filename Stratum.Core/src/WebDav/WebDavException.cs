// Copyright (C) 2024 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;

namespace Stratum.Core.WebDav
{
    public class WebDavException : Exception
    {
        public int StatusCode { get; }

        public WebDavException(string message, int statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public WebDavException(string message, int statusCode, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
