using System;
using System.Collections.Generic;
using System.Linq;

namespace KillitDesktopAuthenticator.Adb
{
    /// <summary>The device-owner workflow on top of <see cref="AdbClient"/>.</summary>
    internal sealed class Provisioner
    {
        public const string KillitPackage = "org.tamodak.killit";
        public const string KillitAdmin = KillitPackage + "/.admin.KillitDeviceAdminReceiver";

        private readonly AdbClient _adb;

        public Provisioner(AdbClient adb)
        {
            _adb = adb;
        }

        public List<AdbDevice> ListDevices()
        {
            var result = _adb.Run("devices", "-l");
            return AdbParsers.ParseDevices(result.Stdout);
        }

        /// <summary>
        /// Checks everything Android verifies before accepting a device owner: Killit is installed,
        /// no owner exists yet, and there are no accounts or extra users.
        /// </summary>
        public CheckResult Check(string serial)
        {
            var installed = AdbParsers.IsPackageInstalled(Shell(serial, "pm path " + KillitPackage).Stdout);
            var owner = GetDeviceOwner(serial);

            var blockers = new List<Blocker>();
            blockers.AddRange(AdbParsers.ParseAccounts(Shell(serial, "dumpsys account").Stdout));
            blockers.AddRange(AdbParsers.ParseExtraUsers(Shell(serial, "pm list users").Stdout));

            return new CheckResult(installed, owner, blockers);
        }

        public string GetDeviceOwner(string serial)
        {
            // dpm list-owners exists from Android 12; older versions print usage instead.
            string owner;
            if (AdbParsers.TryParseListOwners(Shell(serial, "dpm list-owners").Combined, out owner))
                return owner;
            return AdbParsers.ParseDumpsysDeviceOwner(Shell(serial, "dumpsys device_policy").Stdout);
        }

        /// <summary>Runs <c>dpm set-device-owner</c> and returns adb's combined output.</summary>
        public AdbResult SetDeviceOwner(string serial) =>
            Shell(serial, "dpm set-device-owner " + KillitAdmin);

        private AdbResult Shell(string serial, string command)
        {
            var args = new[] { "-s", serial, "shell" }.Concat(command.Split(' ')).ToArray();
            var result = _adb.Run(args);
            if (result.TimedOut)
                throw new DeviceUnavailableException("adb " + string.Join(" ", args) + ": timed out");

            // Errors from adb itself (device unplugged, unauthorized, offline) as opposed to the
            // command's own output, which must not be mistaken for "nothing found".
            var stderr = result.Stderr.Trim();
            if (result.ExitCode != 0 && (stderr.StartsWith("error:", StringComparison.Ordinal)
                                         || stderr.StartsWith("adb: error", StringComparison.Ordinal)))
                throw new DeviceUnavailableException(stderr);
            return result;
        }
    }

    /// <summary>The selected phone stopped answering (unplugged, locked out, offline).</summary>
    internal sealed class DeviceUnavailableException : Exception
    {
        public DeviceUnavailableException(string message)
            : base(message)
        {
        }
    }
}
