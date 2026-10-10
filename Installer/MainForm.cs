// MIMESIS Patches Installer - main window
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MimesisInstaller
{
    sealed class MainForm : Form
    {
        static readonly Color Bg = Color.FromArgb(24, 24, 24);
        static readonly Color Panel = Color.FromArgb(34, 34, 34);
        static readonly Color Fg = Color.FromArgb(232, 232, 226);
        static readonly Color Dim = Color.FromArgb(150, 152, 146);
        static readonly Color Accent = Color.FromArgb(217, 140, 30);
        static readonly Color Good = Color.FromArgb(110, 190, 110);

        readonly Label _gameLabel = new Label();
        readonly ListView _list = new ListView();
        readonly Button _install = new Button(), _uninstall = new Button(), _uninstallAll = new Button(), _browse = new Button();
        readonly ProgressBar _progress = new ProgressBar();
        readonly TextBox _log = new TextBox();
        readonly Label _status = new Label();

        string _dll;
        Dictionary<string, Release> _releases = new Dictionary<string, Release>();
        bool _busy;

        public MainForm()
        {
            Text = $"MIMESIS Patches Installer v{Engine.Version}";
            ClientSize = new Size(820, 640);
            MinimumSize = new Size(700, 520);
            BackColor = Bg;
            ForeColor = Fg;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterScreen;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            var title = new Label { Text = "MIMESIS Patches", Font = new Font("Segoe UI Semibold", 16f), AutoSize = true, Location = new Point(18, 14), ForeColor = Fg };
            var byline = new LinkLabel { Text = "by Mohamed Darwesh (@medovanx) · github.com/medovanx/mimesis-patches", AutoSize = true, Location = new Point(20, 48), LinkColor = Dim, ActiveLinkColor = Accent, LinkBehavior = LinkBehavior.HoverUnderline };
            byline.LinkClicked += (s, e) => Open($"https://github.com/{Engine.Repo}");

            _gameLabel.AutoSize = false;
            _gameLabel.Location = new Point(20, 78);
            _gameLabel.Size = new Size(660, 22);
            _gameLabel.ForeColor = Dim;
            _gameLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Style(_browse, "Game folder...", false);
            _browse.Location = new Point(690, 74);
            _browse.Size = new Size(112, 28);
            _browse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _browse.Click += (s, e) => Browse();

            _list.View = View.Details;
            _list.CheckBoxes = true;
            _list.FullRowSelect = true;
            _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _list.BackColor = Panel;
            _list.ForeColor = Fg;
            _list.BorderStyle = BorderStyle.None;
            _list.Location = new Point(18, 112);
            _list.Size = new Size(784, 290);
            _list.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _list.Columns.Add("Patch", 130);
            _list.Columns.Add("What it does", 300);
            _list.Columns.Add("Who needs it", 100);
            _list.Columns.Add("Latest", 80);
            _list.Columns.Add("Status", 150);
            _list.ItemChecked += (s, e) => UpdateButtons();

            Style(_install, "Install / update selected", true);
            Style(_uninstall, "Uninstall selected", false);
            Style(_uninstallAll, "Uninstall all", false);
            _install.SetBounds(18, 414, 220, 36);
            _uninstall.SetBounds(248, 414, 170, 36);
            _uninstallAll.SetBounds(428, 414, 140, 36);
            _install.Click += async (s, e) => await Run("Installing", dll => Engine.Install(dll, Checked(), _releases, Progress));
            _uninstall.Click += async (s, e) => await Run("Removing", dll => Engine.Uninstall(dll, Checked(), _releases, Progress));
            _uninstallAll.Click += async (s, e) =>
            {
                if (MessageBox.Show(this, "Remove every patch and restore the original game files?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    await Run("Removing all", dll => Engine.Uninstall(dll, Engine.Patches, _releases, Progress));
            };
            var hint = new Label { Text = "MIMESIS is closed automatically while patching.", AutoSize = true, ForeColor = Dim, Location = new Point(580, 424), Anchor = AnchorStyles.Top | AnchorStyles.Right };

            _progress.SetBounds(18, 460, 784, 10);
            _progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.BackColor = Color.FromArgb(16, 16, 16);
            _log.ForeColor = Dim;
            _log.BorderStyle = BorderStyle.None;
            _log.Font = new Font("Consolas", 9f);
            _log.SetBounds(18, 480, 784, 124);
            _log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            _status.AutoSize = false;
            _status.SetBounds(18, 612, 784, 20);
            _status.ForeColor = Dim;
            _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Controls.AddRange(new Control[] { title, byline, _gameLabel, _browse, _list, _install, _uninstall, _uninstallAll, hint, _progress, _log, _status });

            // Route the install logic's console output into the log box.
            Console.SetOut(new LogWriter(this));
            Shown += async (s, e) => await Startup();
        }

        static void Style(Button b, string text, bool primary)
        {
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = primary ? Accent : Color.FromArgb(70, 70, 70);
            b.BackColor = primary ? Accent : Color.FromArgb(48, 48, 48);
            b.ForeColor = primary ? Color.Black : Color.FromArgb(232, 232, 226);
            b.Cursor = Cursors.Hand;
        }

        async Task Startup()
        {
            SetGame(Engine.FindDll(AppContext.BaseDirectory));
            SetBusy(true, "Checking GitHub for the latest patches...");
            try
            {
                _releases = await Task.Run(Engine.LatestReleases);
                var newer = Engine.NewerInstaller(_releases);
                if (newer != null && !Environment.GetCommandLineArgs().Contains("--no-update")
                    && MessageBox.Show(this, $"A newer installer (v{newer.Version.ToString(3)}) is available. Download and open it now?", Text,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    await Task.Run(() => Engine.LaunchNewer(newer, Array.Empty<string>()));
                    Close();
                    return;
                }
                SetBusy(false, "Ready.");
            }
            catch (Exception e)
            {
                SetBusy(false, Program.Describe(e));
            }
            Fill();
        }

        void SetGame(string dll)
        {
            _dll = dll;
            _gameLabel.Text = dll != null
                ? "Game: " + Path.GetFullPath(Path.Combine(Path.GetDirectoryName(dll), "..", ".."))
                : "Game not found. Put this exe next to MIMESIS.exe, or choose the game folder.";
            _gameLabel.ForeColor = dll != null ? Dim : Accent;
        }

        void Browse()
        {
            using var dialog = new FolderBrowserDialog { Description = "Choose the MIMESIS game folder (the one with MIMESIS.exe)", UseDescriptionForTitle = true };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            var dll = Engine.FindDll(dialog.SelectedPath);
            if (dll == null) { MessageBox.Show(this, "That folder doesn't look like MIMESIS (no MIMESIS_Data\\Managed\\Assembly-CSharp.dll).", Text); return; }
            SetGame(dll);
            Fill();
        }

        void Fill()
        {
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var p in Engine.Patches)
            {
                _releases.TryGetValue(p.Name, out var r);
                bool installed = _dll != null && Engine.IsInstalled(_dll, p);
                var item = new ListViewItem(p.Name) { Tag = p, Checked = r != null && !installed || installed };
                item.SubItems.Add(p.About);
                item.SubItems.Add(p.Who);
                item.SubItems.Add(r != null ? "v" + r.Version.ToString(3) : "not released");
                item.SubItems.Add(installed ? "✓ Installed" : "Not installed");
                item.UseItemStyleForSubItems = false;
                item.SubItems[4].ForeColor = installed ? Good : Dim;
                item.SubItems[2].ForeColor = item.SubItems[3].ForeColor = Dim;
                if (r == null) item.ForeColor = Dim;
                _list.Items.Add(item);
            }
            _list.EndUpdate();
            UpdateButtons();
        }

        List<PatchInfo> Checked() => _list.Items.Cast<ListViewItem>().Where(i => i.Checked).Select(i => (PatchInfo)i.Tag).ToList();

        void UpdateButtons()
        {
            bool ready = !_busy && _dll != null;
            var sel = _list.Items.Cast<ListViewItem>().Where(i => i.Checked).Select(i => (PatchInfo)i.Tag).ToList();
            _install.Enabled = ready && sel.Any(p => _releases.ContainsKey(p.Name));
            _uninstall.Enabled = ready && sel.Any(p => Engine.IsInstalled(_dll, p));
            _uninstallAll.Enabled = ready && Engine.Patches.Any(p => Engine.IsInstalled(_dll, p));
            _browse.Enabled = !_busy;
            _list.Enabled = !_busy;
        }

        void SetBusy(bool busy, string status)
        {
            _busy = busy;
            _status.Text = status;
            _progress.Style = busy ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
            if (!busy) _progress.Value = 0;
            UpdateButtons();
        }

        void Progress(int done, int total) => BeginInvoke(() =>
        {
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Maximum = Math.Max(total, 1);
            _progress.Value = Math.Min(done, _progress.Maximum);
        });

        async Task Run(string what, Action<string> work)
        {
            _log.Clear();
            SetBusy(true, what + "...");
            try
            {
                await Task.Run(() => work(_dll));
                SetBusy(false, "Done. Open the game and click \"Patches\" on the main menu to configure them.");
                _progress.Value = _progress.Maximum;
            }
            catch (Exception e)
            {
                Console.WriteLine("\nFAILED: " + Program.Describe(e));
                SetBusy(false, "Failed: " + Program.Describe(e));
            }
            Fill();
        }

        static void Open(string url)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }

        // Console output -> log box (thread-safe).
        sealed class LogWriter : TextWriter
        {
            readonly MainForm _form;
            public LogWriter(MainForm form) => _form = form;
            public override Encoding Encoding => Encoding.UTF8;
            public override void Write(char value) => Write(value.ToString());
            public override void Write(string value)
            {
                if (string.IsNullOrEmpty(value) || _form.IsDisposed) return;
                value = value.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                if (_form.InvokeRequired) _form.BeginInvoke(() => _form._log.AppendText(value));
                else _form._log.AppendText(value);
            }
            public override void WriteLine(string value) => Write(value + "\n");
        }
    }
}
