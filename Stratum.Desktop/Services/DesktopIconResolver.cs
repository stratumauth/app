// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using Stratum.Core;

namespace Stratum.Desktop.Services
{
    public class DesktopIconResolver : IIconResolver
    {
        // Map of common service names to their icon keys
        private static readonly Dictionary<string, string> ServiceIcons = new(StringComparer.OrdinalIgnoreCase)
        {
            // Popular services
            { "google", "google" },
            { "github", "github" },
            { "microsoft", "microsoft" },
            { "amazon", "amazon" },
            { "aws", "aws" },
            { "facebook", "facebook" },
            { "twitter", "twitter" },
            { "discord", "discord" },
            { "dropbox", "dropbox" },
            { "slack", "slack" },
            { "steam", "steam" },
            { "twitch", "twitch" },
            { "reddit", "reddit" },
            { "linkedin", "linkedin" },
            { "paypal", "paypal" },
            { "stripe", "stripe" },
            { "digitalocean", "digitalocean" },
            { "cloudflare", "cloudflare" },
            { "gitlab", "gitlab" },
            { "bitbucket", "bitbucket" },
            { "heroku", "heroku" },
            { "npm", "npm" },
            { "docker", "docker" },
            { "protonmail", "protonmail" },
            { "proton", "proton" },
            { "bitwarden", "bitwarden" },
            { "lastpass", "lastpass" },
            { "1password", "1password" },
            { "dashlane", "dashlane" },
            { "coinbase", "coinbase" },
            { "binance", "binance" },
            { "kraken", "kraken" },
            { "epic games", "epicgames" },
            { "epicgames", "epicgames" },
            { "ubisoft", "ubisoft" },
            { "ea", "ea" },
            { "origin", "origin" },
            { "blizzard", "blizzard" },
            { "battle.net", "battlenet" },
            { "nintendo", "nintendo" },
            { "playstation", "playstation" },
            { "xbox", "xbox" },
            { "zoom", "zoom" },
            { "teams", "teams" },
            { "adobe", "adobe" },
            { "apple", "apple" },
            { "icloud", "icloud" },
            { "spotify", "spotify" },
            { "netflix", "netflix" },
            { "hulu", "hulu" },
            { "namecheap", "namecheap" },
            { "godaddy", "godaddy" },
            { "hover", "hover" },
            { "gandi", "gandi" },
            { "vultr", "vultr" },
            { "linode", "linode" },
            { "hetzner", "hetzner" },
            { "ovh", "ovh" },
            { "backblaze", "backblaze" },
            { "wasabi", "wasabi" },
            { "mongodb", "mongodb" },
            { "atlassian", "atlassian" },
            { "jira", "jira" },
            { "confluence", "confluence" },
            { "trello", "trello" },
            { "notion", "notion" },
            { "figma", "figma" },
            { "canva", "canva" },
            { "mailchimp", "mailchimp" },
            { "sendgrid", "sendgrid" },
            { "mailgun", "mailgun" },
            { "twilio", "twilio" },
            { "intercom", "intercom" },
            { "zendesk", "zendesk" },
            { "hubspot", "hubspot" },
            { "salesforce", "salesforce" },
            { "quickbooks", "quickbooks" },
            { "xero", "xero" },
            { "freshbooks", "freshbooks" },
            { "wise", "wise" },
            { "transferwise", "transferwise" },
            { "revolut", "revolut" },
            { "n26", "n26" },
            { "monzo", "monzo" },
        };

        public string FindServiceKeyByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            // Direct match
            if (ServiceIcons.TryGetValue(name, out var icon))
            {
                return icon;
            }

            // Partial match
            var lowerName = name.ToLowerInvariant();
            var match = ServiceIcons.FirstOrDefault(kvp =>
                lowerName.Contains(kvp.Key) || kvp.Key.Contains(lowerName));

            return match.Value;
        }
    }
}
