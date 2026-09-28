using System.Collections.Generic;

namespace KillitDesktopAuthenticator.Adb
{
    /// <summary>One row of <c>adb devices -l</c>.</summary>
    internal sealed class AdbDevice
    {
        public AdbDevice(string serial, string state, string model, string product)
        {
            Serial = serial;
            State = state;
            Model = model;
            Product = product;
        }

        public string Serial { get; }

        /// <summary>Raw adb state: "device", "unauthorized", "offline", "recovery", …</summary>
        public string State { get; }

        public string Model { get; }
        public string Product { get; }

        /// <summary>Only a device in the "device" state accepts shell commands.</summary>
        public bool IsReady => State == "device";

        public bool IsUnauthorized => State == "unauthorized";
    }

    internal enum BlockerKind
    {
        Account,
        User,
    }

    /// <summary>Something that makes Android refuse <c>dpm set-device-owner</c>.</summary>
    internal sealed class Blocker
    {
        public Blocker(BlockerKind kind, string name, string detail)
        {
            Kind = kind;
            Name = name;
            Detail = detail;
        }

        public BlockerKind Kind { get; }

        /// <summary>Account name (usually an e-mail) or user/profile name.</summary>
        public string Name { get; }

        /// <summary>Account type (e.g. com.google) or user id.</summary>
        public string Detail { get; }
    }

    internal sealed class CheckResult
    {
        public CheckResult(bool killitInstalled, string existingOwnerPackage, IList<Blocker> blockers)
        {
            KillitInstalled = killitInstalled;
            ExistingOwnerPackage = existingOwnerPackage;
            Blockers = blockers;
        }

        public bool KillitInstalled { get; }

        /// <summary>Package of the current device owner, or null when there is none.</summary>
        public string ExistingOwnerPackage { get; }

        public IList<Blocker> Blockers { get; }

        public bool CanAuthenticate =>
            KillitInstalled && ExistingOwnerPackage == null && Blockers.Count == 0;
    }

    /// <summary>Known reasons <c>dpm set-device-owner</c> fails, mapped to a user-facing hint.</summary>
    internal enum SetOwnerFailure
    {
        Unknown,
        AccountsPresent,
        UsersPresent,
        OwnerAlreadySet,
        MissingSecurityPermission,
        UnknownAdmin,
    }

    internal sealed class AdbResult
    {
        public AdbResult(int exitCode, string stdout, string stderr, bool timedOut)
        {
            ExitCode = exitCode;
            Stdout = stdout;
            Stderr = stderr;
            TimedOut = timedOut;
        }

        public int ExitCode { get; }
        public string Stdout { get; }
        public string Stderr { get; }
        public bool TimedOut { get; }

        public string Combined => (Stdout + "\n" + Stderr).Trim();
    }
}
