using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace KillitDesktopAuthenticator.Adb
{
    /// <summary>Thrown when adb itself cannot be started, as opposed to a command failing.</summary>
    internal sealed class AdbUnavailableException : Exception
    {
        public AdbUnavailableException(string message, bool runtimeMissing)
            : base(message)
        {
            RuntimeMissing = runtimeMissing;
        }

        /// <summary>
        /// True when Windows could not load adb's DLLs, which on Windows 8 / 8.1 means the
        /// Universal C Runtime (KB2999226) is not installed.
        /// </summary>
        public bool RuntimeMissing { get; }
    }

    /// <summary>Runs the bundled adb.exe. Every call is blocking; callers run it off the UI thread.</summary>
    internal sealed class AdbClient
    {
        // NTSTATUS values a process exits with when the loader fails before main() runs.
        private const int StatusDllNotFound = unchecked((int)0xC0000135);
        private const int StatusEntryPointNotFound = unchecked((int)0xC0000139);
        private const int StatusInvalidImageFormat = unchecked((int)0xC000007B);

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        private readonly string _adbPath;

        public AdbClient(string adbPath)
        {
            _adbPath = adbPath;
        }

        /// <summary>adb.exe shipped next to the program by the installer.</summary>
        public static string BundledPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "adb", "adb.exe");

        public AdbResult Run(params string[] args) => Run(DefaultTimeout, args);

        public AdbResult Run(TimeSpan timeout, params string[] args)
        {
            if (!File.Exists(_adbPath))
                throw new AdbUnavailableException("adb.exe not found: " + _adbPath, false);

            var psi = new ProcessStartInfo
            {
                FileName = _adbPath,
                Arguments = JoinArguments(args),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = Path.GetDirectoryName(_adbPath),
            };

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            using (var outDone = new ManualResetEvent(false))
            using (var errDone = new ManualResetEvent(false))
            using (var process = new Process { StartInfo = psi })
            {
                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data == null) outDone.Set();
                    else lock (stdout) stdout.AppendLine(e.Data);
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data == null) errDone.Set();
                    else lock (stderr) stderr.AppendLine(e.Data);
                };

                try
                {
                    process.Start();
                }
                catch (Exception e)
                {
                    throw new AdbUnavailableException(e.Message, false);
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (!process.WaitForExit((int)timeout.TotalMilliseconds))
                {
                    try { process.Kill(); } catch (InvalidOperationException) { }
                    return new AdbResult(-1, stdout.ToString(), stderr.ToString(), true);
                }

                // The first adb call may spawn the adb server, which inherits our pipes and keeps
                // them open, so don't wait forever for end-of-stream.
                outDone.WaitOne(2000);
                errDone.WaitOne(2000);

                var exit = process.ExitCode;
                if (exit == StatusDllNotFound || exit == StatusEntryPointNotFound)
                    throw new AdbUnavailableException(
                        "adb.exe could not load its runtime (exit 0x" + exit.ToString("X8") + ")", true);
                if (exit == StatusInvalidImageFormat)
                    throw new AdbUnavailableException("adb.exe is damaged or incompatible", false);

                lock (stdout) lock (stderr)
                    return new AdbResult(exit, stdout.ToString(), stderr.ToString(), false);
            }
        }

        /// <summary>Quotes arguments per the MSVC command line rules adb's CRT uses.</summary>
        internal static string JoinArguments(string[] args)
        {
            var sb = new StringBuilder();
            foreach (var arg in args)
            {
                if (sb.Length > 0)
                    sb.Append(' ');
                if (arg.Length > 0 && arg.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
                {
                    sb.Append(arg);
                    continue;
                }

                sb.Append('"');
                var backslashes = 0;
                foreach (var c in arg)
                {
                    if (c == '\\')
                    {
                        backslashes++;
                        continue;
                    }
                    if (c == '"')
                        sb.Append('\\', backslashes * 2 + 1);
                    else
                        sb.Append('\\', backslashes);
                    backslashes = 0;
                    sb.Append(c);
                }
                sb.Append('\\', backslashes * 2);
                sb.Append('"');
            }
            return sb.ToString();
        }
    }
}
