// Copyright (C) 2024 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Stratum.Core.WebDav
{
    public class WebDavClient : IDisposable
    {
        private static readonly XNamespace DavNs = "DAV:";

        private const string PropfindBody =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <d:propfind xmlns:d="DAV:">
              <d:prop>
                <d:displayname/>
                <d:getlastmodified/>
                <d:getcontentlength/>
                <d:resourcetype/>
              </d:prop>
            </d:propfind>
            """;

        private readonly HttpClient _httpClient;

        public WebDavClient(string baseUrl, string username, string password)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentException("Base URL is required", nameof(baseUrl));
            }

            if (!baseUrl.EndsWith('/'))
            {
                baseUrl += "/";
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), "");
                request.Headers.Add("Depth", "0");
                request.Content = new StringContent(PropfindBody, Encoding.UTF8, "application/xml");

                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                return response.StatusCode == HttpStatusCode.MultiStatus ||
                       response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task EnsureDirectoryAsync(string path)
        {
            path = NormalizePath(path);

            var request = new HttpRequestMessage(new HttpMethod("MKCOL"), path);
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            // 405 Method Not Allowed or 409 Conflict means the directory already exists
            if (response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.Conflict)
            {
                return;
            }

            // 201 Created is success
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new WebDavException(
                $"MKCOL failed: {response.StatusCode} {body}",
                (int) response.StatusCode);
        }

        public async Task UploadFileAsync(string path, byte[] data)
        {
            path = NormalizePath(path);

            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClient.PutAsync(path, content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new WebDavException(
                    $"PUT failed: {response.StatusCode} {body}",
                    (int) response.StatusCode);
            }
        }

        public async Task<byte[]> DownloadFileAsync(string path)
        {
            path = NormalizePath(path);

            var response = await _httpClient.GetAsync(path).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new WebDavException(
                    $"GET failed: {response.StatusCode} {body}",
                    (int) response.StatusCode);
            }

            return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        public async Task<List<WebDavEntry>> ListDirectoryAsync(string path)
        {
            path = NormalizePath(path);

            var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), path);
            request.Headers.Add("Depth", "1");
            request.Content = new StringContent(PropfindBody, Encoding.UTF8, "application/xml");

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.MultiStatus && !response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new WebDavException(
                    $"PROPFIND failed: {response.StatusCode} {errorBody}",
                    (int) response.StatusCode);
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return ParsePropfindResponse(body, path);
        }

        public async Task DeleteAsync(string path)
        {
            path = NormalizePath(path);

            var response = await _httpClient.DeleteAsync(path).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new WebDavException(
                    $"DELETE failed: {response.StatusCode} {body}",
                    (int) response.StatusCode);
            }
        }

        private static List<WebDavEntry> ParsePropfindResponse(string xml, string requestPath)
        {
            var doc = XDocument.Parse(xml);
            var entries = new List<WebDavEntry>();

            var responses = doc.Descendants(DavNs + "response");

            foreach (var resp in responses)
            {
                var href = resp.Element(DavNs + "href")?.Value ?? "";
                var propStat = resp.Element(DavNs + "propstat");
                var prop = propStat?.Element(DavNs + "prop");

                if (prop == null)
                {
                    continue;
                }

                // Skip the directory itself (the request path)
                var decodedHref = Uri.UnescapeDataString(href).TrimEnd('/');
                var decodedRequestPath = Uri.UnescapeDataString(requestPath).TrimEnd('/');

                if (decodedHref == decodedRequestPath ||
                    decodedHref.EndsWith(decodedRequestPath, StringComparison.OrdinalIgnoreCase) &&
                    decodedHref.Length - decodedRequestPath.Length <= 1)
                {
                    continue;
                }

                var isDirectory = prop.Element(DavNs + "resourcetype")
                    ?.Element(DavNs + "collection") != null;

                var lastModifiedStr = prop.Element(DavNs + "getlastmodified")?.Value;
                var lastModified = DateTime.MinValue;

                if (!string.IsNullOrEmpty(lastModifiedStr))
                {
                    DateTime.TryParse(lastModifiedStr, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out lastModified);
                }

                var contentLengthStr = prop.Element(DavNs + "getcontentlength")?.Value;
                long.TryParse(contentLengthStr, out var contentLength);

                var name = prop.Element(DavNs + "displayname")?.Value;

                if (string.IsNullOrEmpty(name))
                {
                    // Fall back to extracting name from href
                    name = Uri.UnescapeDataString(href.TrimEnd('/').Split('/').Last());
                }

                entries.Add(new WebDavEntry
                {
                    Name = name,
                    Href = href,
                    LastModified = lastModified,
                    ContentLength = contentLength,
                    IsDirectory = isDirectory
                });
            }

            return entries;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }

            // Remove leading slash since HttpClient will resolve relative to BaseAddress
            return path.TrimStart('/');
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
