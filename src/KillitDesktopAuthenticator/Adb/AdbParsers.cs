using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KillitDesktopAuthenticator.Adb
{
    /// <summary>
    /// Pure parsers for adb and Android shell output. Kept free of I/O so they can be unit tested
    /// against captured output from real devices.
    /// </summary>
    internal static class AdbParsers
    {
        private static readonly char[] Whitespace = { ' ', '\t' };

        /// <summary>Parses <c>adb devices -l</c>.</summary>
        public static List<AdbDevice> ParseDevices(string output)
        {
            var devices = new List<AdbDevice>();
            foreach (var raw in SplitLines(output))
            {
                var line = raw.Trim();
                // Header and the "* daemon started successfully" chatter adb prints on first use.
                if (line.Length == 0 || line.StartsWith("List of devices", StringComparison.Ordinal)
                    || line.StartsWith("*", StringComparison.Ordinal)
                    || line.StartsWith("adb ", StringComparison.Ordinal))
                    continue;

                var tokens = line.Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 2)
                    continue;

                string model = null, product = null;
                for (var i = 2; i < tokens.Length; i++)
                {
                    var colon = tokens[i].IndexOf(':');
                    if (colon <= 0)
                        continue;
                    var key = tokens[i].Substring(0, colon);
                    var value = tokens[i].Substring(colon + 1);
                    if (key == "model")
                        model = value.Replace('_', ' ');
                    else if (key == "product")
                        product = value;
                }

                devices.Add(new AdbDevice(tokens[0], tokens[1], model, product));
            }
            return devices;
        }

        private static readonly Regex AccountLine =
            new Regex(@"^\s*Account \{name=(.*), type=([^,}]*)\}\s*$", RegexOptions.CultureInvariant);

        private static readonly Regex AccountsHeader =
            new Regex(@"^\s*Accounts:\s*\d+\s*$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Parses <c>dumpsys account</c>. Only the entries directly under an "Accounts: N" header are
        /// taken, so accounts mentioned elsewhere in the dump (sessions, visibility) are not
        /// double-counted.
        /// </summary>
        public static List<Blocker> ParseAccounts(string output)
        {
            var result = new List<Blocker>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var inAccounts = false;

            foreach (var line in SplitLines(output))
            {
                if (AccountsHeader.IsMatch(line))
                {
                    inAccounts = true;
                    continue;
                }
                if (!inAccounts)
                    continue;

                var m = AccountLine.Match(line);
                if (!m.Success)
                {
                    inAccounts = false;
                    continue;
                }

                var name = m.Groups[1].Value;
                var type = m.Groups[2].Value;
                if (seen.Add(type + "\n" + name))
                    result.Add(new Blocker(BlockerKind.Account, name, type));
            }
            return result;
        }

        // UserInfo{10:Work profile:1030} — the name may itself contain ':', the flags are hex.
        private static readonly Regex UserInfo =
            new Regex(@"UserInfo\{(\d+):(.*):([0-9a-fA-F]+)\}", RegexOptions.CultureInvariant);

        /// <summary>
        /// Parses <c>pm list users</c> and returns every user other than the system user (0):
        /// secondary users, guests and work profiles all block device owner provisioning.
        /// </summary>
        public static List<Blocker> ParseExtraUsers(string output)
        {
            var result = new List<Blocker>();
            foreach (var line in SplitLines(output))
            {
                var m = UserInfo.Match(line);
                if (!m.Success)
                    continue;
                var id = m.Groups[1].Value;
                if (id == "0")
                    continue;
                result.Add(new Blocker(BlockerKind.User, m.Groups[2].Value, id));
            }
            return result;
        }

        /// <summary><c>pm path &lt;package&gt;</c> prints "package:/data/app/…" when installed.</summary>
        public static bool IsPackageInstalled(string pmPathOutput)
        {
            foreach (var line in SplitLines(pmPathOutput))
                if (line.Trim().StartsWith("package:", StringComparison.Ordinal))
                    return true;
            return false;
        }

        private static readonly Regex ListOwnersDeviceOwner =
            new Regex(@"admin=([^/\s,]+)/[^,\s]*,[^\r\n]*DeviceOwner", RegexOptions.CultureInvariant);

        /// <summary>
        /// Parses <c>dpm list-owners</c> (Android 12+). Returns false when the output is not
        /// recognised — older Android versions answer with the usage text instead — so the caller
        /// can fall back to <see cref="ParseDumpsysDeviceOwner"/>.
        /// </summary>
        public static bool TryParseListOwners(string output, out string ownerPackage)
        {
            ownerPackage = null;
            if (output == null)
                return false;

            var m = ListOwnersDeviceOwner.Match(output);
            if (m.Success)
            {
                ownerPackage = m.Groups[1].Value;
                return true;
            }

            var trimmed = output.Trim();
            if (trimmed.Equals("no owners", StringComparison.OrdinalIgnoreCase)
                || Regex.IsMatch(trimmed, @"^\d+ owners?:", RegexOptions.CultureInvariant))
                return true;

            return false;
        }

        private static readonly Regex DumpsysDeviceOwner = new Regex(
            @"Device Owner:\s*\r?\n\s*admin=ComponentInfo\{([^/}]+)/",
            RegexOptions.CultureInvariant);

        /// <summary>Parses <c>dumpsys device_policy</c> for the device owner package.</summary>
        public static string ParseDumpsysDeviceOwner(string output)
        {
            if (output == null)
                return null;
            var m = DumpsysDeviceOwner.Match(output);
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>
        /// <c>dpm set-device-owner</c> prints "Success: Device owner set to package …" on success.
        /// </summary>
        public static bool IsSetOwnerSuccess(string output) =>
            output != null && output.IndexOf("Success", StringComparison.Ordinal) >= 0
            && output.IndexOf("Exception", StringComparison.Ordinal) < 0;

        /// <summary>Maps the exception text of a failed <c>dpm set-device-owner</c> to a hint.</summary>
        public static SetOwnerFailure ClassifySetOwnerFailure(string output)
        {
            if (string.IsNullOrEmpty(output))
                return SetOwnerFailure.Unknown;

            // Messages from DevicePolicyManagerService's provisioning precondition checks.
            if (Contains(output, "already some accounts"))
                return SetOwnerFailure.AccountsPresent;
            if (Contains(output, "several users"))
                return SetOwnerFailure.UsersPresent;
            // "Trying to set the device owner (…), but device owner (…) is already set."
            if (Contains(output, "is already set") || Contains(output, "already has a profile owner")
                || Contains(output, "already provisioned"))
                return SetOwnerFailure.OwnerAlreadySet;
            if (Contains(output, "Unknown admin"))
                return SetOwnerFailure.UnknownAdmin;
            // Xiaomi/MIUI/HyperOS and a few others need an extra developer toggle
            // ("USB debugging (Security settings)") before the shell may manage device admins.
            if (Contains(output, "MANAGE_DEVICE_ADMINS") || Contains(output, "SecurityException"))
                return SetOwnerFailure.MissingSecurityPermission;

            return SetOwnerFailure.Unknown;
        }

        private static bool Contains(string haystack, string needle) =>
            haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

        private static IEnumerable<string> SplitLines(string text) =>
            (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }
}
