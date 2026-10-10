// MIMESIS Patches Installer - main window
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Dark, custom-drawn UI: each patch is a card with a toggle, description, "who needs it" tag, version and
// install status.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MimesisInstaller
{
    static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(22, 27, 32);
        public static readonly Color Card = Color.FromArgb(150, 30, 36, 43);
        public static readonly Color CardHover = Color.FromArgb(175, 42, 50, 59);
        public static readonly Color CardOn = Color.FromArgb(185, 58, 50, 32);
        public static readonly Color Border = Color.FromArgb(120, 90, 102, 114);
        public static readonly Color Fg = Color.FromArgb(238, 236, 228);
        public static readonly Color Dim = Color.FromArgb(158, 168, 178);
        public static readonly Color Accent = Color.FromArgb(240, 196, 92);
        public static readonly Color Good = Color.FromArgb(96, 196, 120);
        public static readonly Font Title = new Font("Segoe UI Semibold", 20f);
        public static readonly Font Body = new Font("Segoe UI", 9.5f);
        public static readonly Font Bold = new Font("Segoe UI Semibold", 11f);
        public static readonly Font Small = new Font("Segoe UI", 8.5f);

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            var p = new GraphicsPath();
            float d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public enum Glyph { None, Check, Warn, Arrow }

        /// <summary>Draws a small vector glyph (check mark, warning dot, arrow) in a size x size box.</summary>
        public static void DrawGlyph(Graphics g, Glyph glyph, float x, float y, float size, Color color)
        {
            if (glyph == Glyph.None) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, Math.Max(1.6f, size * 0.16f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            switch (glyph)
            {
                case Glyph.Check:
                    g.DrawLines(pen, new[] { new PointF(x + size * 0.15f, y + size * 0.55f), new PointF(x + size * 0.42f, y + size * 0.8f), new PointF(x + size * 0.88f, y + size * 0.22f) });
                    break;
                case Glyph.Warn:
                    g.DrawEllipse(pen, x + size * 0.08f, y + size * 0.08f, size * 0.84f, size * 0.84f);
                    g.DrawLine(pen, x + size * 0.5f, y + size * 0.28f, x + size * 0.5f, y + size * 0.56f);
                    using (var b = new SolidBrush(color)) g.FillEllipse(b, x + size * 0.42f, y + size * 0.66f, size * 0.16f, size * 0.16f);
                    break;
                case Glyph.Arrow:
                    g.DrawLine(pen, x + size * 0.12f, y + size * 0.5f, x + size * 0.85f, y + size * 0.5f);
                    g.DrawLines(pen, new[] { new PointF(x + size * 0.58f, y + size * 0.22f), new PointF(x + size * 0.88f, y + size * 0.5f), new PointF(x + size * 0.58f, y + size * 0.78f) });
                    break;
            }
        }

        public static void Pill(Graphics g, string text, Font font, Color fg, Color bg, float x, float y, out float width, Glyph glyph = Glyph.None)
        {
            var size = g.MeasureString(text, font);
            float icon = glyph == Glyph.None ? 0 : size.Height - 2;
            width = size.Width + 14 + (icon > 0 ? icon + 2 : 0);
            var r = new RectangleF(x, y, width, size.Height + 4);
            using (var path = Round(r, r.Height / 2)) using (var b = new SolidBrush(bg)) g.FillPath(b, path);
            DrawGlyph(g, glyph, x + 7, y + 3, icon, fg);
            using (var t = new SolidBrush(fg)) g.DrawString(text, font, t, x + 7 + (icon > 0 ? icon + 2 : 0), y + 2);
        }
    }

    /// <summary>One patch: click anywhere to toggle.</summary>
    sealed class PatchCard : Control
    {
        public PatchInfo Patch;
        public string Version = "";
        public bool Installed, Available = true, Checked;
        public event Action Toggled;
        bool _hover;

        public PatchCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Height = 74;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); }
        protected override void OnClick(EventArgs e)
        {
            if (!Available && !Installed) return;
            Checked = !Checked;
            Invalidate();
            Toggled?.Invoke();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            bool usable = Available || Installed;
            using (var path = Theme.Round(r, 10))
            {
                using (var b = new SolidBrush(Checked ? Theme.CardOn : _hover && usable ? Theme.CardHover : Theme.Card)) g.FillPath(b, path);
                using (var pen = new Pen(Checked ? Color.FromArgb(120, Theme.Accent) : Theme.Border)) g.DrawPath(pen, path);
            }

            // Toggle switch
            var sw = new RectangleF(16, 25, 38, 22);
            using (var path = Theme.Round(sw, 11)) using (var b = new SolidBrush(Checked ? Theme.Accent : Color.FromArgb(84, 85, 95))) g.FillPath(b, path);
            float knob = Checked ? sw.Right - 19 : sw.X + 3;
            using (var b = new SolidBrush(usable ? Color.White : Theme.Dim)) g.FillEllipse(b, knob, sw.Y + 3, 16, 16);

            float x = 70;
            using (var fg = new SolidBrush(usable ? Theme.Fg : Theme.Dim)) g.DrawString(Patch.Name, Theme.Bold, fg, x, 13);
            float nameW = g.MeasureString(Patch.Name, Theme.Bold).Width;
            Theme.Pill(g, Patch.Who, Theme.Small, Theme.Dim, Color.FromArgb(70, 71, 80), x + nameW + 6, 16, out _);
            using (var dim = new SolidBrush(Theme.Dim)) g.DrawString(Patch.About, Theme.Body, dim, x + 1, 40);

            // Right side: version and status
            // Installed: green check. Available to download: gold with an arrow so it stands out. Otherwise grey.
            bool download = !Installed && Available;
            string status = Installed ? "Installed" : download ? "Download" : "Coming soon";
            var statusSize = g.MeasureString(status, Theme.Small);
            float sx = Width - statusSize.Width - 30 - (Installed || download ? statusSize.Height : 0);
            Color pillFg = Installed ? Theme.Good : download ? Theme.Accent : Theme.Dim;
            Color pillBg = Installed ? Color.FromArgb(30, 60, 38) : download ? Color.FromArgb(72, 58, 24) : Color.FromArgb(66, 67, 76);
            Theme.Pill(g, status, Theme.Small, pillFg, pillBg, sx, 14, out float pillW,
                Installed ? Theme.Glyph.Check : download ? Theme.Glyph.Arrow : Theme.Glyph.None);
            if (download)
                using (var pen = new Pen(Color.FromArgb(150, Theme.Accent), 1f))
                using (var path = Theme.Round(new RectangleF(sx, 14, pillW, statusSize.Height + 4), (statusSize.Height + 4) / 2))
                    g.DrawPath(pen, path);
            if (!string.IsNullOrEmpty(Version))
            {
                var vs = g.MeasureString(Version, Theme.Small);
                using (var dim = new SolidBrush(Theme.Dim)) g.DrawString(Version, Theme.Small, dim, Width - vs.Width - 16, 44);
            }
        }
    }

    /// <summary>Flat rounded button with hover.</summary>
    sealed class FlatButton : Control
    {
        public bool Primary;
        bool _hover;
        public FlatButton(string text, bool primary)
        {
            Text = text;
            Primary = primary;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI Semibold", 10f);
            Size = new Size(200, 40);
        }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            Color bg = !Enabled ? Color.FromArgb(50, 51, 57)
                : Primary ? (_hover ? Color.FromArgb(250, 214, 120) : Theme.Accent)
                : (_hover ? Color.FromArgb(74, 75, 84) : Color.FromArgb(60, 61, 69));
            Color fg = !Enabled ? Color.FromArgb(80, 80, 88) : Primary ? Color.FromArgb(40, 28, 8) : Theme.Fg;
            using (var path = Theme.Round(r, 8))
            {
                if (Primary && Enabled)
                    using (var gb = new LinearGradientBrush(r, _hover ? Color.FromArgb(255, 230, 150) : Color.FromArgb(250, 220, 130),
                                                              _hover ? Color.FromArgb(236, 178, 64) : Color.FromArgb(222, 160, 50), 90f))
                        g.FillPath(gb, path);
                else
                using (var b = new SolidBrush(bg)) g.FillPath(b, path);
                if (!Primary && Enabled) using (var pen = new Pen(Theme.Border)) g.DrawPath(pen, path);
            }
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    /// <summary>Thin accent progress bar.</summary>
    sealed class SlimProgress : Control
    {
        float _value; bool _indeterminate; float _phase;
        readonly Timer _timer = new Timer { Interval = 30 };
        public SlimProgress()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Height = 4;
            _timer.Tick += (s, e) => { _phase = (_phase + 0.015f) % 1.3f; Invalidate(); };
        }
        public void Set(float value) { _indeterminate = false; _timer.Stop(); _value = Math.Clamp(value, 0f, 1f); Invalidate(); }
        public void Busy() { _indeterminate = true; _timer.Start(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.FromArgb(40, 47, 55));
            using var b = new SolidBrush(Theme.Accent);
            if (_indeterminate) g.FillRectangle(b, (_phase - 0.3f) * Width, 0, Width * 0.3f, Height);
            else g.FillRectangle(b, 0, 0, Width * _value, Height);
        }
    }

    /// <summary>Text with a drawn glyph in front (check, warning, ...).</summary>
    sealed class GlyphLabel : Control
    {
        Theme.Glyph _glyph;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Theme.Glyph Glyph { get => _glyph; set { _glyph = value; Invalidate(); } }
        public GlyphLabel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnForeColorChanged(EventArgs e) { base.OnForeColorChanged(e); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            float h = Font.GetHeight(g);
            float x = 0;
            if (Glyph != Theme.Glyph.None) { Theme.DrawGlyph(g, Glyph, 0, 1, h, ForeColor); x = h + 6; }
            TextRenderer.DrawText(g, Text, Font, new Rectangle((int)x, 0, Width - (int)x, Height), ForeColor,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>"MIMESIS" in wide white capitals with a few horizontally shifted slices, like the game's logo.</summary>
    sealed class GlitchTitle : Control
    {
        float _wordEnd;

        public GlitchTitle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(700, 72);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var font = new Font("Segoe UI Black", 36f, FontStyle.Regular);
            const string word = "MIMESIS";
            using var bmp = new Bitmap(Width, 70);
            using (var bg = Graphics.FromImage(bmp))
            {
                bg.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                float x = 0;
                foreach (var ch in word)
                {
                    bg.DrawString(ch.ToString(), font, Brushes.White, x, 0);
                    x += bg.MeasureString(ch.ToString(), font).Width * 0.92f + 10;   // wide letter spacing
                }
                _wordEnd = x;
            }
            // Glitch: copy the logo in horizontal strips, nudging a few sideways.
            int[] shifts = { 0, 0, 7, 0, -5, 0, 0, 9, 0, -3, 0, 0, 4, 0 };
            int strip = bmp.Height / shifts.Length + 1;
            for (int i = 0; i < shifts.Length; i++)
            {
                var src = new Rectangle(0, i * strip, bmp.Width, strip);
                g.DrawImage(bmp, new Rectangle(shifts[i], i * strip, bmp.Width, strip), src, GraphicsUnit.Pixel);
            }
            using var gold = new SolidBrush(Theme.Accent);
            using var small = new Font("Segoe UI Black", 18f);
            // On the same line, right after MIMESIS, sitting on its baseline.
            g.DrawString("P A T C H E S", small, gold, _wordEnd + 12, 30);
        }
    }

    sealed class MainForm : Form
    {
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        readonly GlyphLabel _game = new GlyphLabel();
        readonly LinkLabel _browse = new LinkLabel(), _selectAll = new LinkLabel();
        readonly FlowLayoutPanel _cards = new FlowLayoutPanel();
        readonly List<PatchCard> _cardList = new List<PatchCard>();
        readonly FlatButton _install = new FlatButton("Install / update selected", true);
        readonly FlatButton _uninstall = new FlatButton("Uninstall selected", false);
        readonly FlatButton _uninstallAll = new FlatButton("Uninstall all", false);
        readonly SlimProgress _progress = new SlimProgress();
        readonly GlyphLabel _status = new GlyphLabel();

        string _dll;
        Dictionary<string, Release> _releases = new Dictionary<string, Release>();
        bool _busy;

        public MainForm()
        {
            Text = $"MIMESIS Patches Installer v{Engine.Version}";
            ClientSize = new Size(900, 756);
            MinimumSize = new Size(760, 600);
            BackColor = Theme.Bg;
            ForeColor = Theme.Fg;
            Font = Theme.Body;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            var title = new GlitchTitle { Location = new Point(28, 16) };
            var sub = new LinkLabel
            {
                Text = $"v{Engine.Version}   |   by Mohamed Darwesh (@medovanx)   |   GitHub",
                AutoSize = true, Location = new Point(31, 92), LinkColor = Theme.Dim, ActiveLinkColor = Theme.Accent,
                LinkBehavior = LinkBehavior.HoverUnderline, ForeColor = Theme.Dim, BackColor = Color.Transparent,
            };
            sub.LinkClicked += (s, e) => Open($"https://github.com/{Engine.Repo}");

            _game.SetBounds(30, 120, 700, 22);
            _game.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Link(_browse, "Change game folder");
            _browse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _browse.Location = new Point(ClientSize.Width - 160, 122);
            _browse.LinkClicked += (s, e) => Browse();

            Link(_selectAll, "Select all");
            _selectAll.Location = new Point(30, 158);
            _selectAll.LinkClicked += (s, e) => SelectAll();

            _cards.SetBounds(22, 182, ClientSize.Width - 44, 440);
            _cards.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _cards.AutoScroll = true;
            _cards.BackColor = Color.Transparent;
            _cards.Padding = new Padding(4, 4, 4, 0);
            _cards.Resize += (s, e) => LayoutCards();

            _install.SetBounds(28, 636, 240, 42);
            _uninstall.SetBounds(280, 636, 190, 42);
            _uninstallAll.SetBounds(482, 636, 150, 42);
            foreach (var b in new Control[] { _install, _uninstall, _uninstallAll }) b.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _install.Click += async (s, e) => await Run("Installing", dll => Engine.Install(dll, Checked(), _releases, Progress));
            _uninstall.Click += async (s, e) => await Run("Removing", dll => Engine.Uninstall(dll, Checked(), _releases, Progress));
            _uninstallAll.Click += async (s, e) =>
            {
                if (MessageBox.Show(this, "Remove every patch? BepInEx stays installed for any other mods.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    await Run("Removing all", dll => Engine.Uninstall(dll, Engine.Patches, _releases, Progress));
            };

            _progress.SetBounds(28, 692, ClientSize.Width - 56, 4);
            _progress.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            _status.SetBounds(28, 706, ClientSize.Width - 56, 40);
            _status.ForeColor = Theme.Dim;
            _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Controls.AddRange(new Control[] { title, sub, _game, _browse, _selectAll, _cards, _install, _uninstall, _uninstallAll, _progress, _status });

            Console.SetOut(new LogWriter(this));   // install logic output -> status line
            Shown += async (s, e) => await Startup();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            var r = ClientRectangle;
            if (r.Width == 0 || r.Height == 0) return;
            using (var b = new LinearGradientBrush(r, Color.FromArgb(34, 42, 48), Color.FromArgb(12, 15, 18), 90f)) g.FillRectangle(b, r);
            // Soft fog patches, like the game's misty menu backdrop.
            g.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (var (fx, fy, fw, fh, a) in new[] { (0.55f, -0.15f, 0.8f, 0.55f, 26), (-0.2f, 0.35f, 0.7f, 0.5f, 16), (0.6f, 0.7f, 0.7f, 0.5f, 12) })
            {
                var fog = new RectangleF(r.Width * fx, r.Height * fy, r.Width * fw, r.Height * fh);
                using var path = new GraphicsPath();
                path.AddEllipse(fog);
                using var pb = new PathGradientBrush(path) { CenterColor = Color.FromArgb(a, 190, 205, 210), SurroundColors = new[] { Color.FromArgb(0, 190, 205, 210) } };
                g.FillEllipse(pb, fog);
            }
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); Invalidate(); }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int on = 1;
            try { DwmSetWindowAttribute(Handle, 20, ref on, 4); } catch { }   // dark title bar (Windows 10 2004+ / 11)
        }

        static void Link(LinkLabel l, string text)
        {
            l.BackColor = Color.Transparent;
            l.Text = text;
            l.AutoSize = true;
            l.LinkColor = Theme.Accent;
            l.ActiveLinkColor = Color.FromArgb(255, 190, 100);
            l.LinkBehavior = LinkBehavior.HoverUnderline;
        }

        async Task Startup()
        {
            SetGame(Engine.DetectDll());
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
                SetBusy(false, _dll != null ? "Ready. Pick your patches and click Install." : "Choose your MIMESIS folder to get started.");
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
            if (dll != null)
            {
                _game.Text = "Game found: " + Path.GetFullPath(Path.Combine(Path.GetDirectoryName(dll), "..", ".."));
                _game.ForeColor = Theme.Good;
                _game.Glyph = Theme.Glyph.Check;
            }
            else
            {
                _game.Text = "MIMESIS not found. Choose your game folder (the one with MIMESIS.exe)";
                _game.ForeColor = Theme.Accent;
                _game.Glyph = Theme.Glyph.Warn;
            }
        }

        void Browse()
        {
            using var dialog = new FolderBrowserDialog { Description = "Choose the MIMESIS game folder (the one with MIMESIS.exe)", UseDescriptionForTitle = true };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            var dll = Engine.FindDll(dialog.SelectedPath);
            if (dll == null) { MessageBox.Show(this, "That folder isn't MIMESIS (no MIMESIS_Data\\Managed\\Assembly-CSharp.dll).", Text); return; }
            SetGame(dll);
            if (!_busy) _status.Text = "Ready. Pick your patches and click Install.";
            Fill();
        }

        void Fill()
        {
            _cards.SuspendLayout();
            _cards.Controls.Clear();
            _cardList.Clear();
            foreach (var p in Engine.Patches)
            {
                _releases.TryGetValue(p.Name, out var r);
                bool installed = _dll != null && Engine.IsInstalled(_dll, p);
                var card = new PatchCard
                {
                    Patch = p,
                    Version = r != null ? "v" + r.Version.ToString(3) : "",
                    Installed = installed,
                    Available = r != null,
                    Checked = r != null,
                    Margin = new Padding(6),
                };
                card.Toggled += UpdateButtons;
                _cardList.Add(card);
                _cards.Controls.Add(card);
            }
            LayoutCards();
            _cards.ResumeLayout();
            UpdateButtons();
        }

        // Two columns of equal-width cards.
        void LayoutCards()
        {
            int inner = _cards.ClientSize.Width - _cards.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
            int w = Math.Max(300, inner / 2 - 12);
            foreach (var c in _cardList) c.Width = w;
            int rows = (_cardList.Count + 1) / 2;
            int content = _cards.Padding.Top + rows * (_cardList.Count > 0 ? _cardList[0].Height + 12 : 0);
            _cards.AutoScroll = content > _cards.ClientSize.Height;
        }

        void SelectAll()
        {
            bool all = _cardList.Where(c => c.Available).All(c => c.Checked);
            foreach (var c in _cardList) { c.Checked = c.Available && !all; c.Invalidate(); }
            UpdateButtons();
        }

        List<PatchInfo> Checked() => _cardList.Where(c => c.Checked).Select(c => c.Patch).ToList();

        void UpdateButtons()
        {
            bool ready = !_busy && _dll != null;
            var sel = _cardList.Where(c => c.Checked).ToList();
            _install.Enabled = ready && sel.Any(c => c.Available);
            _uninstall.Enabled = ready && sel.Any(c => c.Installed);
            _uninstallAll.Enabled = ready && _cardList.Any(c => c.Installed);
            _selectAll.Text = _cardList.Where(c => c.Available).All(c => c.Checked) ? "Select none" : "Select all";
            _browse.Enabled = !_busy;
            _cards.Enabled = !_busy;
        }

        void SetBusy(bool busy, string status)
        {
            _busy = busy;
            _status.Text = status;
            bool bad = status.StartsWith("Failed") || status.StartsWith("Couldn't");
            _status.ForeColor = bad ? Theme.Accent : Theme.Dim;
            _status.Glyph = bad ? Theme.Glyph.Warn : Theme.Glyph.None;
            if (busy) _progress.Busy(); else _progress.Set(0);
            UpdateButtons();
        }

        void Progress(int done, int total) => BeginInvoke(() => _progress.Set(total == 0 ? 1 : done / (float)total));

        async Task Run(string what, Action<string> work)
        {
            if (System.Diagnostics.Process.GetProcessesByName("MIMESIS").Length > 0 &&
                MessageBox.Show(this, "MIMESIS is running. Close it now to continue?", "MIMESIS Patches",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;
            SetBusy(true, what + "...");
            try
            {
                await Task.Run(() => work(_dll));
                SetBusy(false, "Done. Start the game and click \"Patches\" on the main menu to set them up.");
                _status.ForeColor = Theme.Good;
                _status.Glyph = Theme.Glyph.Check;
                _progress.Set(1);
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

        // Install logic output -> the status line shows the latest step (thread-safe).
        sealed class LogWriter : TextWriter
        {
            readonly MainForm _form;
            readonly StringBuilder _line = new StringBuilder();
            public LogWriter(MainForm form) => _form = form;
            public override Encoding Encoding => Encoding.UTF8;
            public override void Write(char value)
            {
                if (value == (char)13) return;
                if (value != (char)10) { _line.Append(value); return; }
                var text = _line.ToString().Trim().TrimStart('=').Trim();
                _line.Clear();
                if (text.Length == 0 || _form.IsDisposed || !_form._busy) return;
                _form.BeginInvoke(() => { if (_form._busy) _form._status.Text = text; });
            }
            public override void Write(string value) { if (value != null) foreach (var c in value) Write(c); }
            public override void WriteLine(string value) => Write(value + Environment.NewLine);
        }
    }
}
