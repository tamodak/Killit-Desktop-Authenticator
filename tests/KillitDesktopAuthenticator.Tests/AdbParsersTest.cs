using KillitDesktopAuthenticator.Adb;
using Xunit;

namespace KillitDesktopAuthenticator.Tests
{
    public class AdbParsersTest
    {
        [Fact]
        public void ParseDevices_readsStateAndModel()
        {
            const string output =
                "* daemon not running; starting now at tcp:5037\r\n" +
                "* daemon started successfully\r\n" +
                "List of devices attached\r\n" +
                "R58N123ABC             device product:beyond1ltexx model:SM_G973F device:beyond1 transport_id:1\r\n" +
                "0123456789ABCDEF       unauthorized usb:1-1 transport_id:2\r\n" +
                "emulator-5554          offline transport_id:3\r\n" +
                "\r\n";

            var devices = AdbParsers.ParseDevices(output);

            Assert.Equal(3, devices.Count);
            Assert.Equal("R58N123ABC", devices[0].Serial);
            Assert.Equal("SM G973F", devices[0].Model);
            Assert.Equal("beyond1ltexx", devices[0].Product);
            Assert.True(devices[0].IsReady);
            Assert.True(devices[1].IsUnauthorized);
            Assert.Null(devices[1].Model);
            Assert.Equal("offline", devices[2].State);
        }

        [Fact]
        public void ParseDevices_emptyList()
        {
            Assert.Empty(AdbParsers.ParseDevices("List of devices attached\n\n"));
        }

        [Fact]
        public void ParseAccounts_onlyTakesTheAccountsBlock()
        {
            const string output =
                "User UserInfo{0:Owner:c13}:\n" +
                "  Accounts: 2\n" +
                "    Account {name=someone@gmail.com, type=com.google}\n" +
                "    Account {name=WhatsApp, type=com.whatsapp}\n" +
                "\n" +
                "  AccountId, Action_Type, timestamp, UID, TableName, Key\n" +
                "  Active Sessions: 0\n" +
                "\n" +
                "  RegisteredServicesCache: 3 services\n" +
                "    ServiceInfo: AuthenticatorDescription {type=com.google}, ComponentInfo{...}, uid 10123\n" +
                "  Account visibility:\n" +
                "    Account {name=someone@gmail.com, type=com.google}\n";

            var accounts = AdbParsers.ParseAccounts(output);

            Assert.Equal(2, accounts.Count);
            Assert.Equal("someone@gmail.com", accounts[0].Name);
            Assert.Equal("com.google", accounts[0].Detail);
            Assert.Equal("WhatsApp", accounts[1].Name);
            Assert.All(accounts, a => Assert.Equal(BlockerKind.Account, a.Kind));
        }

        [Fact]
        public void ParseAccounts_none()
        {
            Assert.Empty(AdbParsers.ParseAccounts("User UserInfo{0:Owner:c13}:\n  Accounts: 0\n\n  Active Sessions: 0\n"));
        }

        [Fact]
        public void ParseExtraUsers_skipsSystemUser()
        {
            const string output =
                "Users:\n" +
                "\tUserInfo{0:Owner:c13} running\n" +
                "\tUserInfo{10:Work profile:1030} running\n" +
                "\tUserInfo{11:Guest: Kid:414}\n";

            var users = AdbParsers.ParseExtraUsers(output);

            Assert.Equal(2, users.Count);
            Assert.Equal("Work profile", users[0].Name);
            Assert.Equal("10", users[0].Detail);
            Assert.Equal("Guest: Kid", users[1].Name);
            Assert.Equal("11", users[1].Detail);
        }

        [Fact]
        public void ParseExtraUsers_onlyOwner()
        {
            Assert.Empty(AdbParsers.ParseExtraUsers("Users:\n\tUserInfo{0:Owner:c13} running\n"));
        }

        [Theory]
        [InlineData("package:/data/app/~~abc==/org.tamodak.killit-xyz==/base.apk\n", true)]
        [InlineData("", false)]
        public void IsPackageInstalled(string output, bool expected)
        {
            Assert.Equal(expected, AdbParsers.IsPackageInstalled(output));
        }

        [Fact]
        public void TryParseListOwners_noOwners()
        {
            Assert.True(AdbParsers.TryParseListOwners("no owners\n", out var owner));
            Assert.Null(owner);
        }

        [Fact]
        public void TryParseListOwners_deviceOwner()
        {
            const string output = "1 owner:\nUser  0: admin=com.example.mdm/.Receiver,DeviceOwner\n";
            Assert.True(AdbParsers.TryParseListOwners(output, out var owner));
            Assert.Equal("com.example.mdm", owner);
        }

        [Fact]
        public void TryParseListOwners_profileOwnerOnlyIsNotADeviceOwner()
        {
            const string output = "1 owner:\nUser 10: admin=com.example.work/.Admin,ProfileOwner\n";
            Assert.True(AdbParsers.TryParseListOwners(output, out var owner));
            Assert.Null(owner);
        }

        [Fact]
        public void TryParseListOwners_unknownCommandFallsBack()
        {
            const string output = "usage: dpm [subcommand] [options]\nusage: dpm set-active-admin ...\n\nError: unknown command 'list-owners'\n";
            Assert.False(AdbParsers.TryParseListOwners(output, out _));
        }

        [Fact]
        public void ParseDumpsysDeviceOwner_found()
        {
            const string output =
                "Current Device Policy Manager state:\n" +
                "  Immutable state:\n" +
                "    Device Owner: \n" +
                "      admin=ComponentInfo{com.example.mdm/com.example.mdm.Receiver}\n" +
                "      name=\n" +
                "      package=com.example.mdm\n";
            Assert.Equal("com.example.mdm", AdbParsers.ParseDumpsysDeviceOwner(output));
        }

        [Fact]
        public void ParseDumpsysDeviceOwner_none()
        {
            const string output =
                "Current Device Policy Manager state:\n" +
                "  Device Owner Type: -1\n" +
                "  Enabled Device Admins (User 0, provisioningState: 0):\n";
            Assert.Null(AdbParsers.ParseDumpsysDeviceOwner(output));
        }

        [Fact]
        public void IsSetOwnerSuccess()
        {
            Assert.True(AdbParsers.IsSetOwnerSuccess(
                "Success: Device owner set to package ComponentInfo{org.tamodak.killit/org.tamodak.killit.admin.KillitDeviceAdminReceiver}\n" +
                "Active admin set to component {org.tamodak.killit/org.tamodak.killit.admin.KillitDeviceAdminReceiver}\n"));
            Assert.False(AdbParsers.IsSetOwnerSuccess(
                "Exception occurred while executing 'set-device-owner':\njava.lang.IllegalStateException: Not allowed to set the device owner because there are already some accounts on the device.\n"));
        }

        [Theory]
        [InlineData("java.lang.IllegalStateException: Not allowed to set the device owner because there are already some accounts on the device.", "AccountsPresent")]
        [InlineData("java.lang.IllegalStateException: Not allowed to set the device owner because there are already several users on the device.", "UsersPresent")]
        [InlineData("java.lang.IllegalStateException: Trying to set the device owner (ComponentInfo{…}), but device owner (ComponentInfo{…}) is already set.", "OwnerAlreadySet")]
        [InlineData("java.lang.IllegalStateException: Trying to set the device owner, but the user already has a profile owner.", "OwnerAlreadySet")]
        [InlineData("java.lang.SecurityException: Neither user 2000 nor current process has android.permission.MANAGE_DEVICE_ADMINS.", "MissingSecurityPermission")]
        [InlineData("java.lang.IllegalArgumentException: Unknown admin: ComponentInfo{org.tamodak.killit/org.tamodak.killit.admin.KillitDeviceAdminReceiver}", "UnknownAdmin")]
        [InlineData("something new", "Unknown")]
        public void ClassifySetOwnerFailure(string output, string expected)
        {
            // By name: SetOwnerFailure is internal, and xunit theories must be public.
            Assert.Equal(expected, AdbParsers.ClassifySetOwnerFailure(output).ToString());
        }

        [Theory]
        [InlineData(new[] { "devices", "-l" }, "devices -l")]
        [InlineData(new[] { "-s", "my serial" }, "-s \"my serial\"")]
        [InlineData(new[] { "a\"b" }, "\"a\\\"b\"")]
        [InlineData(new[] { "" }, "\"\"")]
        public void JoinArguments(string[] args, string expected)
        {
            Assert.Equal(expected, AdbClient.JoinArguments(args));
        }
    }
}
