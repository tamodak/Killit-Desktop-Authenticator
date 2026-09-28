using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using KillitDesktopAuthenticator.Adb;
using KillitDesktopAuthenticator.Properties;

namespace KillitDesktopAuthenticator
{
    /// <summary>
    /// The single window: pick a phone, Check it, remove what blocks provisioning, Authenticate.
    /// Every adb call runs on a worker thread while the buttons are disabled.
    /// </summary>
    internal sealed class MainForm : Form
    {
        private const string WebsiteUrl = "https://tamodak.org";

        private readonly Provisioner _provisioner;

        private readonly ListView _devices = NewListView();
        private readonly ListView _blockers = NewListView();
        private readonly Button _refresh = NewButton(Strings.Refresh);
        private readonly Button _check = NewButton(Strings.Check);
        private readonly Button _authenticate = NewButton(Strings.Authenticate);
        private readonly TextBox _status = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = SystemColors.Window,
        };
        private readonly LinkLabel _website = new LinkLabel
        {
            Text = Strings.WebsiteLink,
            AutoSize = true,
            Visible = false,
            Margin = new Padding(3, 0, 3, 6),
        };

        private bool _busy;

        /// <summary>
        /// Serial of the phone whose last Check found nothing in the way. Authenticate is only
        /// enabled while this phone is the one selected.
        /// </summary>
        private string _readySerial;

        public MainForm(Provisioner provisioner)
        {
            _provisioner = provisioner;

            Text = Strings.AppTitle;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(760, 660);
            MinimumSize = new Size(560, 520);
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (ArgumentException)
            {
            }

            if (CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
            {
                RightToLeft = RightToLeft.Yes;
                RightToLeftLayout = true;
            }

            _devices.Columns.Add(Strings.ColPhone, 200);
            _devices.Columns.Add(Strings.ColSerial, 170);
            _devices.Columns.Add(Strings.ColState, 300);
            _blockers.Columns.Add(Strings.ColKind, 140);
            _blockers.Columns.Add(Strings.ColName, 290);
            _blockers.Columns.Add(Strings.ColDetail, 240);

            BuildLayout();

            _refresh.Click += async (s, e) => await RefreshDevicesAsync();
            _check.Click += async (s, e) => await CheckAsync();
            _authenticate.Click += async (s, e) => await AuthenticateAsync();
            _devices.SelectedIndexChanged += (s, e) => UpdateButtons();
            _website.LinkClicked += (s, e) => OpenWebsite();
            Shown += async (s, e) => await RefreshDevicesAsync();

            UpdateButtons();
        }

        private void BuildLayout()
        {
            var intro = new Label
            {
                Text = Strings.Intro,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 3, 3, 9),
            };

            var devicesButtons = ButtonRow(_refresh, _check);
            var devicesGroup = Group(Strings.DevicesGroup, _devices, devicesButtons);

            var hint = new Label { Text = Strings.BlockersHint, AutoSize = true, Dock = DockStyle.Fill };
            var blockersGroup = Group(Strings.BlockersGroup, hint, _blockers);

            var authGroup = Group(Strings.AuthenticateGroup, ButtonRow(_authenticate));

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(9),
                ColumnCount = 1,
                RowCount = 6,
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(intro, 0, 0);
            root.Controls.Add(devicesGroup, 0, 1);
            root.Controls.Add(blockersGroup, 0, 2);
            root.Controls.Add(authGroup, 0, 3);
            root.Controls.Add(_status, 0, 4);
            root.Controls.Add(_website, 0, 5);
            Controls.Add(root);

            // Wrap the intro and hint text to the window width instead of one endless line.
            Resize += (s, e) => WrapLabels(intro, hint);
            Load += (s, e) => WrapLabels(intro, hint);
        }

        private void WrapLabels(params Label[] labels)
        {
            var width = Math.Max(200, ClientSize.Width - 40);
            foreach (var label in labels)
                label.MaximumSize = new Size(width, 0);
        }

        private async Task RefreshDevicesAsync()
        {
            ShowStatus(Strings.StatusSearching);
            var previous = SelectedDevice?.Serial;

            var devices = await RunAdb(() => _provisioner.ListDevices());
            if (devices == null)
                return;

            _devices.BeginUpdate();
            _devices.Items.Clear();
            foreach (var device in devices)
            {
                var item = new ListViewItem(device.Model ?? device.Product ?? device.Serial) { Tag = device };
                item.SubItems.Add(device.Serial);
                item.SubItems.Add(DescribeState(device));
                if (!device.IsReady)
                    item.ForeColor = SystemColors.GrayText;
                _devices.Items.Add(item);
            }
            _devices.EndUpdate();

            // Keep the user's selection across refreshes; preselect when there is only one phone.
            foreach (ListViewItem item in _devices.Items)
                if (((AdbDevice)item.Tag).Serial == previous)
                    item.Selected = true;
            if (_devices.SelectedItems.Count == 0 && _devices.Items.Count == 1)
                _devices.Items[0].Selected = true;

            if (_readySerial != null && !devices.Exists(d => d.Serial == _readySerial && d.IsReady))
            {
                _readySerial = null;
                _blockers.Items.Clear();
            }

            ShowStatus(devices.Count == 0
                ? Strings.StatusNoDevices
                : string.Format(Strings.StatusDevicesFound, devices.Count));
            UpdateButtons();
        }

        private async Task CheckAsync()
        {
            var device = SelectedDevice;
            if (device == null || !device.IsReady)
                return;

            _readySerial = null;
            _blockers.Items.Clear();
            ShowStatus(Strings.StatusChecking);

            var result = await RunAdb(() => _provisioner.Check(device.Serial));
            if (result == null)
                return;

            _blockers.BeginUpdate();
            foreach (var blocker in result.Blockers)
            {
                var isAccount = blocker.Kind == BlockerKind.Account;
                var item = new ListViewItem(isAccount ? Strings.KindAccount : Strings.KindUser);
                item.SubItems.Add(blocker.Name);
                item.SubItems.Add(isAccount ? blocker.Detail : string.Format(Strings.UserDetail, blocker.Detail));
                _blockers.Items.Add(item);
            }
            _blockers.EndUpdate();

            if (!result.KillitInstalled)
                ShowStatus(Strings.StatusKillitMissing, showWebsite: true);
            else if (result.ExistingOwnerPackage != null)
                ShowStatus(string.Format(Strings.StatusOwnerExists, result.ExistingOwnerPackage));
            else if (result.Blockers.Count > 0)
                ShowStatus(string.Format(Strings.StatusBlockersFound, result.Blockers.Count));
            else
            {
                _readySerial = device.Serial;
                ShowStatus(Strings.StatusReady);
            }
            UpdateButtons();
        }

        private async Task AuthenticateAsync()
        {
            var device = SelectedDevice;
            if (device == null || device.Serial != _readySerial)
                return;

            ShowStatus(Strings.StatusAuthenticating);
            string owner = null;
            var result = await RunAdb(() =>
            {
                var r = _provisioner.SetDeviceOwner(device.Serial);
                if (AdbParsers.IsSetOwnerSuccess(r.Combined))
                    owner = _provisioner.GetDeviceOwner(device.Serial);
                return r;
            });
            if (result == null)
                return;

            // Whatever happened, the phone's state has changed; a new Check is needed before retrying.
            _readySerial = null;

            if (AdbParsers.IsSetOwnerSuccess(result.Combined))
            {
                ShowStatus(owner == Provisioner.KillitPackage ? Strings.StatusSuccess : Strings.StatusVerifyFailed);
            }
            else
            {
                var failure = AdbParsers.ClassifySetOwnerFailure(result.Combined);
                var text = Strings.StatusFailed;
                var hint = HintFor(failure);
                if (hint != null)
                    text += Environment.NewLine + hint;
                text += Environment.NewLine + Environment.NewLine + Strings.PhoneOutput
                        + Environment.NewLine + result.Combined;
                ShowStatus(text, showWebsite: failure == SetOwnerFailure.UnknownAdmin);
            }
            UpdateButtons();
        }

        private static string HintFor(SetOwnerFailure failure)
        {
            switch (failure)
            {
                case SetOwnerFailure.AccountsPresent: return Strings.HintAccounts;
                case SetOwnerFailure.UsersPresent: return Strings.HintUsers;
                case SetOwnerFailure.OwnerAlreadySet: return Strings.HintOwnerSet;
                case SetOwnerFailure.MissingSecurityPermission: return Strings.HintSecurity;
                case SetOwnerFailure.UnknownAdmin: return Strings.HintUnknownAdmin;
                default: return null;
            }
        }

        /// <summary>
        /// Runs adb work off the UI thread with the buttons disabled. Returns null (after showing
        /// the error) when adb or the phone could not be reached.
        /// </summary>
        private async Task<T> RunAdb<T>(Func<T> work) where T : class
        {
            _busy = true;
            UpdateButtons();
            UseWaitCursor = true;
            try
            {
                return await Task.Run(work);
            }
            catch (AdbUnavailableException e)
            {
                ShowStatus(e.RuntimeMissing
                    ? Strings.StatusRuntimeMissing
                    : string.Format(Strings.StatusAdbMissing, e.Message));
            }
            catch (DeviceUnavailableException e)
            {
                _readySerial = null;
                ShowStatus(string.Format(Strings.StatusDeviceLost, e.Message));
            }
            catch (Exception e)
            {
                _readySerial = null;
                ShowStatus(string.Format(Strings.StatusUnexpected, e.Message));
            }
            finally
            {
                _busy = false;
                UseWaitCursor = false;
                UpdateButtons();
            }
            return null;
        }

        private AdbDevice SelectedDevice =>
            _devices.SelectedItems.Count == 1 ? (AdbDevice)_devices.SelectedItems[0].Tag : null;

        private void UpdateButtons()
        {
            var device = SelectedDevice;
            _refresh.Enabled = !_busy;
            _check.Enabled = !_busy && device != null && device.IsReady;
            _authenticate.Enabled = !_busy && device != null && device.Serial == _readySerial
                                    && _blockers.Items.Count == 0;
        }

        private void ShowStatus(string text, bool showWebsite = false)
        {
            _status.Text = text.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            _website.Visible = showWebsite;
        }

        private static string DescribeState(AdbDevice device)
        {
            if (device.IsReady)
                return Strings.StateReady;
            if (device.IsUnauthorized)
                return Strings.StateUnauthorized;
            if (device.State == "offline")
                return Strings.StateOffline;
            return string.Format(Strings.StateOther, device.State);
        }

        private void OpenWebsite()
        {
            try
            {
                Process.Start(WebsiteUrl);
            }
            catch (Exception)
            {
                // No default browser registered; the address is visible in the link text anyway.
            }
        }

        private static ListView NewListView() => new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            GridLines = true,
            Dock = DockStyle.Fill,
        };

        private static Button NewButton(string text) => new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(110, 30),
            Padding = new Padding(8, 2, 8, 2),
        };

        private static FlowLayoutPanel ButtonRow(params Button[] buttons)
        {
            var row = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
            };
            row.Controls.AddRange(buttons);
            return row;
        }

        /// <summary>A group box that stacks its children; the ListView (if any) takes the spare height.</summary>
        private static GroupBox Group(string title, params Control[] children)
        {
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = children.Length };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var autoSized = true;
            foreach (var child in children)
            {
                var fills = child is ListView;
                table.RowStyles.Add(fills ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
                autoSized &= !fills;
                table.Controls.Add(child);
            }
            var group = new GroupBox { Text = title, Dock = DockStyle.Fill, Padding = new Padding(6) };
            if (autoSized)
            {
                table.AutoSize = true;
                group.AutoSize = true;
                group.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            }
            group.Controls.Add(table);
            return group;
        }
    }
}
