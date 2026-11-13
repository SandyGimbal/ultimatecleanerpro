using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: AssemblyTitle("Ultimate Deep Cleaner Pro - Sandy Edition")]
[assembly: AssemblyDescription("Aplikasi Pembersih Mendalam Windows milik Sandy (IG: @sandy_gimbal | TikTok: Sandy.gimbal)")]
[assembly: AssemblyCompany("Sandy (@sandy_gimbal | TikTok: Sandy.gimbal)")]
[assembly: AssemblyProduct("Ultimate Deep Cleaner Pro - Sandy Edition")]
[assembly: AssemblyCopyright("Copyright © Sandy | IG: @sandy_gimbal | TikTok: Sandy.gimbal")]
[assembly: AssemblyVersion("3.1.0.0")]
[assembly: AssemblyFileVersion("3.1.0.0")]

namespace UltimateDeepCleaner
{
    // =========================================================================
    // NATIVE C++ WIN32 KERNEL API IMPORTS & SOUND SYNTHESIZER
    // =========================================================================
    internal static class NativeWin32
    {
        [DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern uint SHEmptyRecycleBinW(IntPtr hwnd, string pszRootPath, uint dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public static bool IsAdministrator()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 MB";
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double val = bytes;
            int idx = 0;
            while (val >= 1024.0 && idx < units.Length - 1)
            {
                val /= 1024.0;
                idx++;
            }
            return string.Format("{0:0.0} {1}", val, units[idx]);
        }

        public static Font PxFont(float pxSize, FontStyle style = FontStyle.Regular)
        {
            return new Font("Segoe UI", pxSize, style, GraphicsUnit.Pixel);
        }

        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0 || r.Width <= d || r.Height <= d)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color Darken(Color c, float factor)
        {
            return Color.FromArgb(
                c.A,
                (int)Math.Max(0, c.R * factor),
                (int)Math.Max(0, c.G * factor),
                (int)Math.Max(0, c.B * factor));
        }

        public static Color Lighten(Color c, int add)
        {
            return Color.FromArgb(
                c.A,
                Math.Min(255, c.R + add),
                Math.Min(255, c.G + add),
                Math.Min(255, c.B + add));
        }

        // Synthesize 44.1kHz Crystal Bell Chime for Scan Complete
        public static void PlayScanCompleteSound()
        {
            Task.Run(() =>
            {
                try
                {
                    PlayChordWav(new double[] { 587.33, 880.0 }, new double[] { 0.0, 0.11 }, 0.55);
                }
                catch { }
            });
        }

        // Synthesize 44.1kHz 4-Note Futuristic Victory Fanfare Chime (C5 -> E5 -> G5 -> C6)
        public static void PlayVictoryFanfareSound()
        {
            Task.Run(() =>
            {
                try
                {
                    PlayChordWav(
                        new double[] { 523.25, 659.25, 783.99, 1046.50 },
                        new double[] { 0.0, 0.13, 0.26, 0.39 },
                        1.25);
                }
                catch
                {
                    try { SystemSounds.Asterisk.Play(); } catch { }
                }
            });
        }

        private static void PlayChordWav(double[] freqs, double[] starts, double durationSeconds)
        {
            int sampleRate = 44100;
            int numSamples = (int)(sampleRate * durationSeconds);
            int dataSize = numSamples * 2;

            using (MemoryStream ms = new MemoryStream(44 + dataSize))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(new char[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + dataSize);
                bw.Write(new char[] { 'W', 'A', 'V', 'E' });
                bw.Write(new char[] { 'f', 'm', 't', ' ' });
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(new char[] { 'd', 'a', 't', 'a' });
                bw.Write(dataSize);

                for (int i = 0; i < numSamples; i++)
                {
                    double t = (double)i / sampleRate;
                    double sample = 0.0;
                    for (int n = 0; n < freqs.Length; n++)
                    {
                        if (t >= starts[n])
                        {
                            double dt = t - starts[n];
                            double decay = (n == freqs.Length - 1) ? 3.8 : 7.0;
                            double env = Math.Exp(-dt * decay) * Math.Min(1.0, dt / 0.008);
                            double wave = Math.Sin(2.0 * Math.PI * freqs[n] * dt)
                                        + 0.35 * Math.Sin(2.0 * Math.PI * (freqs[n] * 2.0) * dt)
                                        + 0.12 * Math.Sin(2.0 * Math.PI * (freqs[n] * 3.0) * dt);
                            sample += wave * env * 0.26;
                        }
                    }
                    if (sample > 0.98) sample = 0.98;
                    if (sample < -0.98) sample = -0.98;
                    bw.Write((short)(sample * 32767));
                }

                ms.Position = 0;
                using (SoundPlayer sp = new SoundPlayer(ms))
                {
                    sp.PlaySync();
                }
            }
        }
    }

    // =========================================================================
    // DATA MODEL FOR CLEANING CATEGORIES & CELEBRATION PARTICLES
    // =========================================================================
    public class CleanTarget
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<string> Paths { get; set; }
        public bool IsChecked { get; set; }
        public bool IsRecycleBin { get; set; }
        public bool IsThumbCache { get; set; }
        public long ScannedBytes { get; set; }
        public int ScannedFiles { get; set; }

        public CleanTarget()
        {
            Paths = new List<string>();
            IsChecked = true;
        }
    }

    public class SparkleParticle
    {
        public float X;
        public float Y;
        public float VX;
        public float VY;
        public float Size;
        public float Alpha;
        public Color ParticleColor;
    }

    // =========================================================================
    // TACTILE GLOSSY BUTTON (WITH EXTRUDED WALL, GLASS SHEEN & PRESS DEPTH)
    // =========================================================================
    public class Button3D : Control
    {
        public Color BaseColor { get; set; }
        public Color TextColor { get; set; }
        public Font CustomFont { get; set; }
        public int CornerRadius { get; set; }
        public int ExtrudeDepth { get; set; }

        private bool isHovered = false;
        private bool isPressed = false;

        public Button3D()
        {
            BaseColor = Color.FromArgb(37, 99, 235);
            TextColor = Color.White;
            CustomFont = NativeWin32.PxFont(13f, FontStyle.Bold);
            CornerRadius = 8;
            ExtrudeDepth = 4;
            this.Cursor = Cursors.Hand;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;

            this.MouseEnter += (s, e) => { isHovered = true; Invalidate(); };
            this.MouseLeave += (s, e) => { isHovered = false; isPressed = false; Invalidate(); };
            this.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { isPressed = true; Invalidate(); } };
            this.MouseUp += (s, e) => { if (isPressed) { isPressed = false; Invalidate(); } };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle r = this.ClientRectangle;
            if (r.Width < 10 || r.Height < 10) return;

            int pressOffset = (isPressed && this.Enabled) ? ExtrudeDepth - 1 : 0;
            int faceHeight = r.Height - ExtrudeDepth - 1;

            Rectangle shadowRect = new Rectangle(2, ExtrudeDepth + 1, r.Width - 4, faceHeight);
            using (GraphicsPath shPath = NativeWin32.RoundedRect(shadowRect, CornerRadius))
            using (SolidBrush shBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            {
                g.FillPath(shBrush, shPath);
            }

            Color activeColor = !this.Enabled ? Color.FromArgb(51, 65, 85) : (isHovered ? NativeWin32.Lighten(BaseColor, 18) : BaseColor);
            Color wallColor = NativeWin32.Darken(activeColor, 0.52f);

            Rectangle wallRect = new Rectangle(1, ExtrudeDepth, r.Width - 3, faceHeight);
            using (GraphicsPath wallPath = NativeWin32.RoundedRect(wallRect, CornerRadius))
            using (SolidBrush wallBrush = new SolidBrush(wallColor))
            {
                g.FillPath(wallBrush, wallPath);
            }

            Rectangle faceRect = new Rectangle(1, pressOffset, r.Width - 3, faceHeight);
            using (GraphicsPath facePath = NativeWin32.RoundedRect(faceRect, CornerRadius))
            {
                using (LinearGradientBrush faceGrad = new LinearGradientBrush(
                    new Point(faceRect.X, faceRect.Y),
                    new Point(faceRect.X, faceRect.Bottom),
                    NativeWin32.Lighten(activeColor, 22),
                    NativeWin32.Darken(activeColor, 0.85f)))
                {
                    g.FillPath(faceGrad, facePath);
                }

                Rectangle glossRect = new Rectangle(faceRect.X + 2, faceRect.Y + 1, faceRect.Width - 4, faceRect.Height / 2);
                using (GraphicsPath glossPath = NativeWin32.RoundedRect(glossRect, Math.Max(2, CornerRadius - 2)))
                using (LinearGradientBrush glossBrush = new LinearGradientBrush(
                    new Point(glossRect.X, glossRect.Y),
                    new Point(glossRect.X, glossRect.Bottom + 1),
                    Color.FromArgb(75, 255, 255, 255),
                    Color.FromArgb(5, 255, 255, 255)))
                {
                    g.FillPath(glossBrush, glossPath);
                }

                using (Pen rimPen = new Pen(Color.FromArgb(isHovered ? 210 : 120, 255, 255, 255), 1.2f))
                {
                    g.DrawPath(rimPen, facePath);
                }
            }

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
            Rectangle txtShadowRect = new Rectangle(faceRect.X + 1, faceRect.Y + 2, faceRect.Width, faceRect.Height);
            TextRenderer.DrawText(g, this.Text, CustomFont, txtShadowRect, Color.FromArgb(4, 9, 20), flags);
            TextRenderer.DrawText(g, this.Text, CustomFont, faceRect, this.Enabled ? TextColor : Color.FromArgb(148, 163, 184), flags);
        }
    }

    // =========================================================================
    // HEADER PANEL WITH PERSPECTIVE CYBER HORIZON GRID
    // =========================================================================
    public class Header3DPanel : Panel
    {
        public Header3DPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = this.ClientRectangle;

            using (LinearGradientBrush bg = new LinearGradientBrush(
                new Point(0, 0),
                new Point(0, r.Height),
                Color.FromArgb(15, 25, 48),
                Color.FromArgb(24, 38, 66)))
            {
                g.FillRectangle(bg, r);
            }

            int vx = r.Width / 2;
            int vy = -30;
            using (Pen gridPen = new Pen(Color.FromArgb(28, 56, 189, 248), 1f))
            {
                for (int x = -300; x <= r.Width + 300; x += 65)
                {
                    g.DrawLine(gridPen, vx, vy, x, r.Height);
                }
                for (int y = 14; y < r.Height; y += 14)
                {
                    g.DrawLine(gridPen, 0, y, r.Width, y);
                }
            }

            using (LinearGradientBrush glowLine = new LinearGradientBrush(
                new Point(0, 0),
                new Point(r.Width, 0),
                Color.FromArgb(40, 56, 189, 248),
                Color.FromArgb(220, 16, 185, 129)))
            using (Pen p = new Pen(glowLine, 2f))
            {
                g.DrawLine(p, 0, r.Height - 2, r.Width, r.Height - 2);
            }
        }
    }

    // =========================================================================
    // CYLINDRICAL LIQUID/PLASMA MINI BAR (FOR DRIVE D: & RAM)
    // =========================================================================
    public class Cylinder3DBar : Control
    {
        public string TitleText { get; set; }
        public string ValueText { get; set; }
        public float Percent { get; set; }
        public Color FillColor { get; set; }

        public Cylinder3DBar()
        {
            TitleText = "";
            ValueText = "";
            Percent = 0f;
            FillColor = Color.FromArgb(56, 189, 248);
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle r = this.ClientRectangle;
            using (SolidBrush clearBg = new SolidBrush(Color.FromArgb(20, 29, 47)))
            {
                g.FillRectangle(clearBg, r);
            }

            Rectangle cardRect = new Rectangle(1, 1, r.Width - 3, r.Height - 3);
            using (GraphicsPath path = NativeWin32.RoundedRect(cardRect, 8))
            {
                using (LinearGradientBrush cardBg = new LinearGradientBrush(
                    new Point(0, 0),
                    new Point(0, r.Height),
                    Color.FromArgb(34, 48, 74),
                    Color.FromArgb(22, 32, 52)))
                {
                    g.FillPath(cardBg, path);
                }
                using (Pen borderPen = new Pen(Color.FromArgb(70, 148, 163, 184), 1f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            using (Font fBold = NativeWin32.PxFont(12f, FontStyle.Bold))
            using (SolidBrush bTitle = new SolidBrush(Color.FromArgb(226, 232, 240)))
            using (SolidBrush bVal = new SolidBrush(NativeWin32.Lighten(FillColor, 35)))
            {
                g.DrawString(TitleText, fBold, bTitle, 10, 6);
                SizeF sz = g.MeasureString(ValueText, fBold);
                g.DrawString(ValueText, fBold, bVal, r.Width - sz.Width - 10, 6);
            }

            Rectangle track = new Rectangle(10, 27, r.Width - 20, 11);
            using (GraphicsPath trackPath = NativeWin32.RoundedRect(track, 5))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(10, 16, 29)))
            using (Pen innerShadow = new Pen(Color.FromArgb(140, 0, 0, 0), 1f))
            {
                g.FillPath(tb, trackPath);
                g.DrawPath(innerShadow, trackPath);
            }

            int fillW = (int)(track.Width * Math.Min(1f, Math.Max(0f, Percent / 100f)));
            if (fillW > 4)
            {
                Rectangle fill = new Rectangle(track.X, track.Y, fillW, track.Height);
                using (GraphicsPath fillPath = NativeWin32.RoundedRect(fill, 5))
                using (LinearGradientBrush fb = new LinearGradientBrush(
                    new Point(fill.X, fill.Y),
                    new Point(fill.X, fill.Bottom),
                    NativeWin32.Lighten(FillColor, 40),
                    NativeWin32.Darken(FillColor, 0.65f)))
                {
                    g.FillPath(fb, fillPath);
                }

                Rectangle spec = new Rectangle(fill.X + 2, fill.Y + 1, Math.Max(2, fill.Width - 4), 3);
                using (SolidBrush spb = new SolidBrush(Color.FromArgb(135, 255, 255, 255)))
                {
                    g.FillRectangle(spb, spec);
                }
            }
        }
    }

    // =========================================================================
    // HOLOGRAPHIC ROTATING SPHERE, STORAGE GAUGE & CELEBRATION BURST (60 FPS)
    // =========================================================================
    public class Holographic3DGauge : Control
    {
        public double TargetUsedPercent { get; set; }
        public double DisplayedUsedPercent { get; set; }
        public string FreeText { get; set; }
        public string UsedText { get; set; }
        public string HealthBadge { get; set; }
        public Color BadgeColor { get; set; }

        public bool IsLoading { get; set; }
        public string LoadingStatus { get; set; }
        public float RadarAngle { get; set; }
        public float Rot3DAngle { get; set; }

        // Celebration Effect State
        public bool IsCelebrating { get; set; }
        public float CelebrationWaveRadius { get; set; }
        public float CelebrationAlpha { get; set; }
        public List<SparkleParticle> Particles = new List<SparkleParticle>();
        private Random rng = new Random();

        public Holographic3DGauge()
        {
            FreeText = "Membaca...";
            UsedText = "";
            HealthBadge = "MEMBACA";
            BadgeColor = Color.FromArgb(56, 189, 248);
            LoadingStatus = "Memindai...";
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public void TriggerCelebrationBurst()
        {
            IsCelebrating = true;
            CelebrationWaveRadius = 30f;
            CelebrationAlpha = 1.0f;
            Particles.Clear();

            float cx = this.Width / 2f;
            float cy = 96f;
            Color[] cols = new Color[]
            {
                Color.FromArgb(16, 185, 129),
                Color.FromArgb(56, 189, 248),
                Color.FromArgb(250, 204, 21),
                Color.White
            };

            for (int i = 0; i < 42; i++)
            {
                double ang = rng.NextDouble() * Math.PI * 2.0;
                float spd = 1.8f + (float)(rng.NextDouble() * 4.5);
                Particles.Add(new SparkleParticle
                {
                    X = cx,
                    Y = cy,
                    VX = (float)Math.Cos(ang) * spd,
                    VY = (float)Math.Sin(ang) * spd,
                    Size = 4f + (float)(rng.NextDouble() * 5f),
                    Alpha = 1.0f,
                    ParticleColor = cols[i % cols.Length]
                });
            }
        }

        public void StepCelebration()
        {
            if (!IsCelebrating) return;

            CelebrationWaveRadius += 4.2f;
            CelebrationAlpha -= 0.018f;

            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                var p = Particles[i];
                p.X += p.VX;
                p.Y += p.VY;
                p.VX *= 0.96f;
                p.VY *= 0.96f;
                p.Alpha -= 0.02f;
                if (p.Alpha <= 0f) Particles.RemoveAt(i);
            }

            if (CelebrationAlpha <= 0f && Particles.Count == 0)
            {
                IsCelebrating = false;
            }
        }

        private PointF Project3D(double x, double y, double z, float cx, float cy, double rotX, double rotY)
        {
            double cosY = Math.Cos(rotY);
            double sinY = Math.Sin(rotY);
            double x1 = x * cosY - z * sinY;
            double z1 = x * sinY + z * cosY;

            double cosX = Math.Cos(rotX);
            double sinX = Math.Sin(rotX);
            double y2 = y * cosX - z1 * sinX;
            double z2 = y * sinX + z1 * cosX;

            double fov = 240.0;
            double scale = fov / (fov + z2 + 95.0);
            return new PointF((float)(cx + x1 * scale), (float)(cy + y2 * scale));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = this.ClientRectangle;
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(20, 29, 47)))
            {
                g.FillRectangle(bgBrush, rect);
            }

            int size = 154;
            int cx = rect.Width / 2;
            int cy = 96;
            Rectangle arcRect = new Rectangle(cx - size / 2, cy - size / 2, size, size);

            // 1. Outer Metallic Recessed Dish & Drop Shadow
            Rectangle outerBezel = new Rectangle(arcRect.X - 12, arcRect.Y - 12, arcRect.Width + 24, arcRect.Height + 24);
            using (LinearGradientBrush bezelGrad = new LinearGradientBrush(
                new Point(outerBezel.X, outerBezel.Y),
                new Point(outerBezel.Right, outerBezel.Bottom),
                Color.FromArgb(40, 56, 88),
                Color.FromArgb(10, 15, 28)))
            {
                g.FillEllipse(bezelGrad, outerBezel);
            }
            using (Pen bezelRim = new Pen(Color.FromArgb(IsCelebrating ? 200 : 90, IsCelebrating ? Color.FromArgb(16, 185, 129) : Color.FromArgb(56, 189, 248)), 1.5f))
            {
                g.DrawEllipse(bezelRim, outerBezel);
            }

            Rectangle innerSphere = new Rectangle(arcRect.X + 8, arcRect.Y + 8, arcRect.Width - 16, arcRect.Height - 16);
            using (SolidBrush cavBrush = new SolidBrush(Color.FromArgb(11, 17, 32)))
            {
                g.FillEllipse(cavBrush, innerSphere);
            }

            // 2. REAL-TIME ROTATING HOLOGRAPHIC WIREFRAME GLOBE IN CENTER
            double rx = Rot3DAngle * 0.7;
            double ry = Rot3DAngle * 1.1;
            double radius3D = 62.0;
            Color wireColor = IsCelebrating
                ? Color.FromArgb(95, 16, 185, 129)
                : (IsLoading ? Color.FromArgb(75, 56, 189, 248) : Color.FromArgb(42, 56, 189, 248));

            using (Pen wirePen = new Pen(wireColor, 1.1f))
            {
                for (int lat = -60; lat <= 60; lat += 30)
                {
                    double phi = lat * Math.PI / 180.0;
                    double yRing = radius3D * Math.Sin(phi);
                    double rRing = radius3D * Math.Cos(phi);
                    PointF[] pts = new PointF[25];
                    for (int i = 0; i <= 24; i++)
                    {
                        double theta = (i * 15.0) * Math.PI / 180.0;
                        double x = rRing * Math.Cos(theta);
                        double z = rRing * Math.Sin(theta);
                        pts[i] = Project3D(x, yRing, z, cx, cy, rx, ry);
                    }
                    g.DrawLines(wirePen, pts);
                }

                for (int lon = 0; lon < 180; lon += 45)
                {
                    double theta = lon * Math.PI / 180.0;
                    PointF[] pts = new PointF[25];
                    for (int i = 0; i <= 24; i++)
                    {
                        double phi = (i * 15.0) * Math.PI / 180.0;
                        double x = radius3D * Math.Sin(phi) * Math.Cos(theta);
                        double y = radius3D * Math.Cos(phi);
                        double z = radius3D * Math.Sin(phi) * Math.Sin(theta);
                        pts[i] = Project3D(x, y, z, cx, cy, rx, ry);
                    }
                    g.DrawLines(wirePen, pts);
                }
            }

            // 3. Radial Dial Tick Marks around Bezel
            for (int i = 0; i <= 27; i++)
            {
                double ang = (135.0 + i * 10.0) * Math.PI / 180.0;
                float r1 = size / 2f + 7f;
                float r2 = size / 2f + 10f;
                float x1 = cx + (float)(Math.Cos(ang) * r1);
                float y1 = cy + (float)(Math.Sin(ang) * r1);
                float x2 = cx + (float)(Math.Cos(ang) * r2);
                float y2 = cy + (float)(Math.Sin(ang) * r2);
                using (Pen tp = new Pen(Color.FromArgb(100, 148, 163, 184), 1.5f))
                {
                    g.DrawLine(tp, x1, y1, x2, y2);
                }
            }

            // 4. Recessed Track Tube
            using (Pen trackShadow = new Pen(Color.FromArgb(180, 5, 10, 20), 15f))
            using (Pen trackPen = new Pen(Color.FromArgb(32, 45, 68), 12f))
            {
                trackShadow.StartCap = LineCap.Round;
                trackShadow.EndCap = LineCap.Round;
                trackPen.StartCap = LineCap.Round;
                trackPen.EndCap = LineCap.Round;
                g.DrawArc(trackShadow, arcRect, 135f, 270f);
                g.DrawArc(trackPen, arcRect, 135f, 270f);
            }

            // 5. Volumetric Neon Progress Tube
            Color mainColor = Color.FromArgb(16, 185, 129);
            if (!IsCelebrating)
            {
                if (DisplayedUsedPercent >= 92.0)
                    mainColor = Color.FromArgb(239, 68, 68);
                else if (DisplayedUsedPercent >= 80.0)
                    mainColor = Color.FromArgb(245, 158, 11);
            }

            float sweep = (float)(270.0 * (Math.Min(100.0, Math.Max(0.0, DisplayedUsedPercent)) / 100.0));
            if (sweep > 0.5f)
            {
                using (Pen glowPen = new Pen(Color.FromArgb(55, mainColor), 20f))
                {
                    glowPen.StartCap = LineCap.Round;
                    glowPen.EndCap = LineCap.Round;
                    g.DrawArc(glowPen, arcRect, 135f, sweep);
                }
                using (Pen valPen = new Pen(mainColor, 12.5f))
                {
                    valPen.StartCap = LineCap.Round;
                    valPen.EndCap = LineCap.Round;
                    g.DrawArc(valPen, arcRect, 135f, sweep);
                }
                using (Pen specPen = new Pen(Color.FromArgb(185, 255, 255, 255), 3.2f))
                {
                    specPen.StartCap = LineCap.Round;
                    specPen.EndCap = LineCap.Round;
                    g.DrawArc(specPen, arcRect, 135f, sweep);
                }

                double tipAng = (135.0 + sweep) * Math.PI / 180.0;
                float tx = cx + (float)(Math.Cos(tipAng) * (size / 2.0));
                float ty = cy + (float)(Math.Sin(tipAng) * (size / 2.0));
                using (SolidBrush tipGlow = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                using (SolidBrush tipCore = new SolidBrush(Color.White))
                {
                    g.FillEllipse(tipGlow, tx - 9, ty - 9, 18, 18);
                    g.FillEllipse(tipCore, tx - 4.5f, ty - 4.5f, 9, 9);
                }
            }

            // 6. Animated Turbo Radar Ring when Scanning/Cleaning
            if (IsLoading)
            {
                Rectangle outerRect = new Rectangle(arcRect.X - 9, arcRect.Y - 9, arcRect.Width + 18, arcRect.Height + 18);
                using (Pen radarGlow = new Pen(Color.FromArgb(90, 56, 189, 248), 8f))
                using (Pen radarPen = new Pen(Color.FromArgb(245, 56, 189, 248), 3.2f))
                {
                    radarGlow.StartCap = LineCap.Round;
                    radarGlow.EndCap = LineCap.Round;
                    radarPen.StartCap = LineCap.Round;
                    radarPen.EndCap = LineCap.Round;
                    g.DrawArc(radarGlow, outerRect, RadarAngle, 85f);
                    g.DrawArc(radarGlow, outerRect, RadarAngle + 180f, 85f);
                    g.DrawArc(radarPen, outerRect, RadarAngle, 85f);
                    g.DrawArc(radarPen, outerRect, RadarAngle + 180f, 85f);
                }
            }

            // 7. Celebration Shockwave Ring & Sparkle Burst Particles
            if (IsCelebrating)
            {
                int wAlpha = (int)(Math.Max(0f, Math.Min(1f, CelebrationAlpha)) * 220);
                if (wAlpha > 5)
                {
                    RectangleF waveRect = new RectangleF(cx - CelebrationWaveRadius, cy - CelebrationWaveRadius, CelebrationWaveRadius * 2, CelebrationWaveRadius * 2);
                    using (Pen wavePen = new Pen(Color.FromArgb(wAlpha, 16, 185, 129), 3.5f))
                    {
                        g.DrawEllipse(wavePen, waveRect);
                    }
                }

                foreach (var p in Particles)
                {
                    int pa = (int)(Math.Max(0f, Math.Min(1f, p.Alpha)) * 255);
                    using (SolidBrush pb = new SolidBrush(Color.FromArgb(pa, p.ParticleColor)))
                    {
                        g.FillEllipse(pb, p.X - p.Size / 2, p.Y - p.Size / 2, p.Size, p.Size);
                    }
                }
            }

            // 8. Glass Dome Reflection over top half of sphere
            Rectangle domeRect = new Rectangle(innerSphere.X + 10, innerSphere.Y + 4, innerSphere.Width - 20, innerSphere.Height / 2);
            using (LinearGradientBrush domeGrad = new LinearGradientBrush(
                new Point(domeRect.X, domeRect.Y),
                new Point(domeRect.X, domeRect.Bottom),
                Color.FromArgb(42, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255)))
            {
                g.FillEllipse(domeGrad, domeRect);
            }

            // 9. Center Percentage Text with Depth Shadow
            string pctStr = string.Format("{0:0.0}%", DisplayedUsedPercent);
            using (Font pctFont = NativeWin32.PxFont(26f, FontStyle.Bold))
            using (SolidBrush shBrush = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
            using (SolidBrush txtBrush = new SolidBrush(Color.White))
            {
                SizeF sz = g.MeasureString(pctStr, pctFont);
                g.DrawString(pctStr, pctFont, shBrush, cx - sz.Width / 2 + 2, cy - sz.Height / 2 - 7);
                g.DrawString(pctStr, pctFont, txtBrush, cx - sz.Width / 2, cy - sz.Height / 2 - 9);
            }

            string subCenter = IsCelebrating ? "✨ BERSIH OPTIMAL!" : (IsLoading ? LoadingStatus : "Partisi Sistem C:");
            using (Font subFont = NativeWin32.PxFont(11.5f, FontStyle.Bold))
            using (SolidBrush subBrush = new SolidBrush(IsCelebrating ? Color.FromArgb(16, 185, 129) : (IsLoading ? Color.FromArgb(56, 189, 248) : Color.FromArgb(148, 163, 184))))
            {
                SizeF sz = g.MeasureString(subCenter, subFont);
                g.DrawString(subCenter, subFont, subBrush, cx - sz.Width / 2, cy + 13);
            }

            // 10. Health Pill Badge
            using (Font badgeFont = NativeWin32.PxFont(10.5f, FontStyle.Bold))
            {
                SizeF bsz = g.MeasureString(HealthBadge, badgeFont);
                Rectangle pillRect = new Rectangle((int)(cx - (bsz.Width + 18) / 2), cy + size / 2 - 14, (int)(bsz.Width + 18), 20);
                using (GraphicsPath pillPath = NativeWin32.RoundedRect(pillRect, 6))
                using (SolidBrush pillBg = new SolidBrush(Color.FromArgb(55, BadgeColor)))
                using (Pen pillPen = new Pen(BadgeColor, 1.2f))
                using (SolidBrush pillTxt = new SolidBrush(NativeWin32.Lighten(BadgeColor, 40)))
                {
                    g.FillPath(pillBg, pillPath);
                    g.DrawPath(pillPen, pillPath);
                    g.DrawString(HealthBadge, badgeFont, pillTxt, pillRect.X + 9, pillRect.Y + 2);
                }
            }

            // 11. Drive C Details below ring
            int yPos = cy + size / 2 + 13;
            using (Font fFree = NativeWin32.PxFont(15.5f, FontStyle.Bold))
            using (SolidBrush bFree = new SolidBrush(Color.White))
            {
                SizeF sz = g.MeasureString(FreeText, fFree);
                g.DrawString(FreeText, fFree, bFree, cx - sz.Width / 2, yPos);
                yPos += (int)sz.Height + 1;
            }

            using (Font fUsed = NativeWin32.PxFont(11.5f, FontStyle.Regular))
            using (SolidBrush bUsed = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                SizeF sz = g.MeasureString(UsedText, fUsed);
                g.DrawString(UsedText, fUsed, bUsed, cx - sz.Width / 2, yPos);
            }
        }
    }

    // =========================================================================
    // BEVELED GLASSMORPHISM CATEGORY CARD
    // =========================================================================
    public class CategoryCard3D : Control
    {
        public CleanTarget Target { get; set; }
        public string SizeText { get; set; }
        public Color SizeColor { get; set; }
        public event EventHandler CheckChanged;

        private CheckBox chk;
        private bool isHovered = false;

        public CategoryCard3D(CleanTarget target)
        {
            this.Target = target;
            this.SizeText = "⏳ Memindai...";
            this.SizeColor = Color.FromArgb(245, 158, 11);
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.Cursor = Cursors.Hand;

            chk = new CheckBox
            {
                Checked = target.IsChecked,
                Location = new Point(16, 17),
                Size = new Size(18, 18),
                BackColor = Color.FromArgb(28, 40, 65),
                Cursor = Cursors.Hand
            };
            chk.CheckedChanged += (s, e) =>
            {
                Target.IsChecked = chk.Checked;
                Invalidate();
                if (CheckChanged != null) CheckChanged(this, EventArgs.Empty);
            };
            this.Controls.Add(chk);

            this.MouseEnter += (s, e) => { isHovered = true; Invalidate(); };
            this.MouseLeave += (s, e) => { isHovered = false; Invalidate(); };
            this.MouseClick += (s, e) => { chk.Checked = !chk.Checked; };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle r = this.ClientRectangle;
            using (SolidBrush clearBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.FillRectangle(clearBrush, r);
            }

            Rectangle shRect = new Rectangle(3, 4, r.Width - 6, r.Height - 5);
            using (GraphicsPath shPath = NativeWin32.RoundedRect(shRect, 9))
            using (SolidBrush shBrush = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
            {
                g.FillPath(shBrush, shPath);
            }

            Rectangle cardRect = new Rectangle(1, 1, r.Width - 4, r.Height - 5);
            using (GraphicsPath cardPath = NativeWin32.RoundedRect(cardRect, 9))
            {
                Color cTop = isHovered ? Color.FromArgb(40, 58, 92) : Color.FromArgb(31, 44, 70);
                Color cBot = isHovered ? Color.FromArgb(26, 38, 62) : Color.FromArgb(20, 30, 50);
                using (LinearGradientBrush bgGrad = new LinearGradientBrush(
                    new Point(0, cardRect.Y),
                    new Point(0, cardRect.Bottom),
                    cTop, cBot))
                {
                    g.FillPath(bgGrad, cardPath);
                }

                Rectangle pillar = new Rectangle(cardRect.X + 2, cardRect.Y + 6, 4, cardRect.Height - 12);
                Color pillarCol = Target.IsChecked ? Color.FromArgb(56, 189, 248) : Color.FromArgb(71, 85, 105);
                using (SolidBrush pb = new SolidBrush(pillarCol))
                {
                    g.FillRectangle(pb, pillar);
                }

                using (Pen borderPen = new Pen(isHovered ? Color.FromArgb(180, 56, 189, 248) : Color.FromArgb(75, 148, 163, 184), 1.1f))
                {
                    g.DrawPath(borderPen, cardPath);
                }
            }

            using (Font fTitle = NativeWin32.PxFont(13.5f, FontStyle.Bold))
            using (SolidBrush bTitle = new SolidBrush(Color.White))
            {
                g.DrawString(Target.Title, fTitle, bTitle, 42, 7);
            }

            using (Font fSub = NativeWin32.PxFont(11.5f, FontStyle.Regular))
            using (SolidBrush bSub = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                g.DrawString(Target.Subtitle, fSub, bSub, 42, 27);
            }

            int pillW = 126;
            int pillH = 28;
            Rectangle pillRect = new Rectangle(cardRect.Right - pillW - 12, cardRect.Y + (cardRect.Height - pillH) / 2, pillW, pillH);
            using (GraphicsPath pillPath = NativeWin32.RoundedRect(pillRect, 7))
            using (SolidBrush pillBg = new SolidBrush(Color.FromArgb(13, 20, 36)))
            using (Pen pillBorder = new Pen(Color.FromArgb(90, SizeColor), 1f))
            {
                g.FillPath(pillBg, pillPath);
                g.DrawPath(pillBorder, pillPath);
            }

            using (Font fSize = NativeWin32.PxFont(13f, FontStyle.Bold))
            {
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
                TextRenderer.DrawText(g, SizeText, fSize, pillRect, SizeColor, flags);
            }
        }
    }

    // =========================================================================
    // CYLINDRICAL ENERGY TUBE LOADING BAR
    // =========================================================================
    public class AnimatedLoadingBar : Control
    {
        public float Progress { get; set; }
        public bool IsIndeterminate { get; set; }
        public float ShimmerOffset { get; set; }
        public string StatusText { get; set; }

        public AnimatedLoadingBar()
        {
            StatusText = "Siap melakukan pembersihan mendalam.";
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle r = this.ClientRectangle;
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.FillRectangle(bg, r);
            }

            using (Font f = NativeWin32.PxFont(13f, FontStyle.Bold))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(226, 232, 240)))
            {
                g.DrawString(StatusText, f, sb, 2, 1);
            }

            if (!IsIndeterminate && Progress > 0.0f)
            {
                string pct = string.Format("{0:0}%", Progress * 100f);
                using (Font f = NativeWin32.PxFont(13f, FontStyle.Bold))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(56, 189, 248)))
                {
                    SizeF sz = g.MeasureString(pct, f);
                    g.DrawString(pct, f, sb, r.Width - sz.Width - 2, 1);
                }
            }

            Rectangle barTrack = new Rectangle(2, 22, r.Width - 4, 14);
            using (GraphicsPath trackPath = NativeWin32.RoundedRect(barTrack, 7))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(10, 16, 29)))
            using (Pen trackRim = new Pen(Color.FromArgb(70, 56, 189, 248), 1f))
            {
                g.FillPath(trackBrush, trackPath);
                g.DrawPath(trackRim, trackPath);
            }

            if (IsIndeterminate)
            {
                int segWidth = Math.Max(90, barTrack.Width / 3);
                int x = barTrack.X + (int)((barTrack.Width + segWidth) * ShimmerOffset) - segWidth;
                Rectangle seg = new Rectangle(x, barTrack.Y + 1, segWidth, barTrack.Height - 2);
                seg.Intersect(barTrack);
                if (seg.Width > 6)
                {
                    using (GraphicsPath segPath = NativeWin32.RoundedRect(seg, 6))
                    using (LinearGradientBrush lgb = new LinearGradientBrush(
                        new Point(seg.Left - 1, seg.Top),
                        new Point(seg.Right + 1, seg.Top),
                        Color.FromArgb(37, 99, 235),
                        Color.FromArgb(56, 189, 248)))
                    {
                        g.FillPath(lgb, segPath);
                    }
                }
            }
            else if (Progress > 0.005f)
            {
                int fillW = (int)(barTrack.Width * Math.Min(1f, Progress));
                if (fillW > 6)
                {
                    Rectangle fillRect = new Rectangle(barTrack.X, barTrack.Y, fillW, barTrack.Height);
                    using (GraphicsPath fillPath = NativeWin32.RoundedRect(fillRect, 7))
                    using (LinearGradientBrush lgb = new LinearGradientBrush(
                        new Point(fillRect.Left, fillRect.Top),
                        new Point(fillRect.Right + 1, fillRect.Top),
                        Color.FromArgb(16, 185, 129),
                        Color.FromArgb(56, 189, 248)))
                    {
                        g.FillPath(lgb, fillPath);
                    }

                    Rectangle topSpecular = new Rectangle(fillRect.X + 4, fillRect.Y + 2, Math.Max(2, fillRect.Width - 8), 4);
                    using (SolidBrush specBrush = new SolidBrush(Color.FromArgb(130, 255, 255, 255)))
                    {
                        g.FillRectangle(specBrush, topSpecular);
                    }

                    int shineW = 80;
                    int shineX = fillRect.X + (int)((fillRect.Width + shineW) * ShimmerOffset) - shineW;
                    Rectangle shineRect = new Rectangle(shineX, fillRect.Y, shineW, fillRect.Height);
                    shineRect.Intersect(fillRect);
                    if (shineRect.Width > 0)
                    {
                        using (SolidBrush shineBrush = new SolidBrush(Color.FromArgb(95, 255, 255, 255)))
                        {
                            g.FillRectangle(shineBrush, shineRect);
                        }
                    }
                }
            }
        }
    }

    // =========================================================================
    // CUSTOM ANIMATED VICTORY CELEBRATION POPUP WINDOW ("EFEK BERHASIL")
    // =========================================================================
    public class VictoryCelebrationForm : Form
    {
        private float animPulse = 0f;
        private List<SparkleParticle> confetti = new List<SparkleParticle>();
        private System.Windows.Forms.Timer timer;
        private Random rng = new Random();
        private string freedDiskText;
        private string deletedFilesText;
        private string freedRamText;
        private string freeSpaceNowText;

        public VictoryCelebrationForm(string freedDisk, int filesCount, string freedRam, string freeNow)
        {
            this.freedDiskText = freedDisk;
            this.deletedFilesText = string.Format("{0:N0} File & Cache", filesCount);
            this.freedRamText = freedRam;
            this.freeSpaceNowText = freeNow;

            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(500, 370);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(13, 21, 38);
            this.DoubleBuffered = true;

            Color[] cols = new Color[]
            {
                Color.FromArgb(16, 185, 129),
                Color.FromArgb(56, 189, 248),
                Color.FromArgb(250, 204, 21),
                Color.FromArgb(236, 72, 153)
            };

            for (int i = 0; i < 38; i++)
            {
                confetti.Add(new SparkleParticle
                {
                    X = (float)(rng.NextDouble() * this.Width),
                    Y = (float)(rng.NextDouble() * this.Height),
                    VX = (float)((rng.NextDouble() - 0.5) * 1.6),
                    VY = -0.8f - (float)(rng.NextDouble() * 1.8),
                    Size = 3f + (float)(rng.NextDouble() * 5f),
                    Alpha = 0.4f + (float)(rng.NextDouble() * 0.6),
                    ParticleColor = cols[i % cols.Length]
                });
            }

            Button3D btnOk = new Button3D
            {
                Text = "✨ MANTAP, SELESAI!",
                CustomFont = NativeWin32.PxFont(15f, FontStyle.Bold),
                BaseColor = Color.FromArgb(16, 185, 129),
                Size = new Size(260, 46),
                Location = new Point((this.Width - 260) / 2, 304)
            };
            btnOk.Click += (s, e) => this.Close();
            this.Controls.Add(btnOk);

            timer = new System.Windows.Forms.Timer { Interval = 25 };
            timer.Tick += (s, e) =>
            {
                animPulse += 0.12f;
                foreach (var p in confetti)
                {
                    p.X += p.VX;
                    p.Y += p.VY;
                    if (p.Y < -10) { p.Y = this.Height + 5; p.X = (float)(rng.NextDouble() * this.Width); }
                }
                this.Invalidate();
            };
            timer.Start();
            this.FormClosed += (s, e) => timer.Stop();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle r = this.ClientRectangle;
            using (LinearGradientBrush bg = new LinearGradientBrush(
                new Point(0, 0),
                new Point(0, r.Height),
                Color.FromArgb(20, 34, 60),
                Color.FromArgb(11, 18, 34)))
            {
                g.FillRectangle(bg, r);
            }

            // Floating celebration particles
            foreach (var p in confetti)
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb((int)(p.Alpha * 210), p.ParticleColor)))
                {
                    g.FillEllipse(sb, p.X, p.Y, p.Size, p.Size);
                }
            }

            // Glowing outer border
            using (Pen borderPen = new Pen(Color.FromArgb(200, 16, 185, 129), 2f))
            {
                g.DrawRectangle(borderPen, 1, 1, r.Width - 3, r.Height - 3);
            }

            // Pulsing Emerald Victory Circle Emblem at Top Center
            int cx = r.Width / 2;
            int cy = 62;
            float pulseR = 36f + (float)Math.Sin(animPulse) * 5f;
            using (SolidBrush aura = new SolidBrush(Color.FromArgb(45, 16, 185, 129)))
            using (SolidBrush aura2 = new SolidBrush(Color.FromArgb(85, 16, 185, 129)))
            using (LinearGradientBrush core = new LinearGradientBrush(
                new Point(cx - 30, cy - 30),
                new Point(cx + 30, cy + 30),
                Color.FromArgb(52, 211, 153),
                Color.FromArgb(5, 150, 105)))
            {
                g.FillEllipse(aura, cx - pulseR - 8, cy - pulseR - 8, (pulseR + 8) * 2, (pulseR + 8) * 2);
                g.FillEllipse(aura2, cx - pulseR, cy - pulseR, pulseR * 2, pulseR * 2);
                g.FillEllipse(core, cx - 30, cy - 30, 60, 60);
            }

            // Draw crisp white Checkmark inside emblem
            using (Pen chkPen = new Pen(Color.White, 5f))
            {
                chkPen.StartCap = LineCap.Round;
                chkPen.EndCap = LineCap.Round;
                chkPen.LineJoin = LineJoin.Round;
                g.DrawLines(chkPen, new PointF[]
                {
                    new PointF(cx - 13, cy + 1),
                    new PointF(cx - 3, cy + 12),
                    new PointF(cx + 15, cy - 10)
                });
            }

            // Headline
            using (Font fHead = NativeWin32.PxFont(20f, FontStyle.Bold))
            using (SolidBrush bHead = new SolidBrush(Color.White))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString("🎉 PEMBERSIHAN MENDALAM BERHASIL!", fHead, bHead, new RectangleF(0, 106, r.Width, 30), sf);
            }

            using (Font fSub = NativeWin32.PxFont(12f, FontStyle.Bold))
            using (SolidBrush bSub = new SolidBrush(Color.FromArgb(250, 204, 21)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString("👑 Ultimate Deep Cleaner Pro  •  @sandy_gimbal | TikTok: Sandy.gimbal", fSub, bSub, new RectangleF(0, 134, r.Width, 22), sf);
            }

            // Stats Box
            Rectangle box = new Rectangle(34, 164, r.Width - 68, 122);
            using (GraphicsPath boxPath = NativeWin32.RoundedRect(box, 10))
            using (SolidBrush boxBg = new SolidBrush(Color.FromArgb(180, 15, 23, 42)))
            using (Pen boxRim = new Pen(Color.FromArgb(90, 56, 189, 248), 1.2f))
            {
                g.FillPath(boxBg, boxPath);
                g.DrawPath(boxRim, boxPath);
            }

            DrawStatLine(g, "🧹 Ruang Penyimpanan Dibebaskan:", freedDiskText, box.X + 18, box.Y + 14, box.Width - 36, Color.FromArgb(16, 185, 129));
            DrawStatLine(g, "🗂️ Total File & Cache Dihapus:", deletedFilesText, box.X + 18, box.Y + 41, box.Width - 36, Color.FromArgb(56, 189, 248));
            DrawStatLine(g, "⚡ Optimasi Memori RAM (C++ API):", freedRamText, box.X + 18, box.Y + 68, box.Width - 36, Color.FromArgb(168, 85, 247));
            DrawStatLine(g, "💾 Ruang Bebas Partisi C: Sekarang:", freeSpaceNowText, box.X + 18, box.Y + 95, box.Width - 36, Color.FromArgb(250, 204, 21));
        }

        private void DrawStatLine(Graphics g, string label, string val, int x, int y, int w, Color valColor)
        {
            using (Font fLbl = NativeWin32.PxFont(12.5f, FontStyle.Regular))
            using (Font fVal = NativeWin32.PxFont(13f, FontStyle.Bold))
            using (SolidBrush bLbl = new SolidBrush(Color.FromArgb(203, 213, 225)))
            using (SolidBrush bVal = new SolidBrush(valColor))
            {
                g.DrawString(label, fLbl, bLbl, x, y);
                SizeF sz = g.MeasureString(val, fVal);
                g.DrawString(val, fVal, bVal, x + w - sz.Width, y - 1);
            }
        }
    }

    // =========================================================================
    // MAIN APPLICATION WINDOW
    // =========================================================================
    public class MainForm : Form
    {
        private Holographic3DGauge gaugeC;
        private Cylinder3DBar miniBarD;
        private Cylinder3DBar miniBarRam;
        private Button3D btnBoostRam;
        private Button3D btnHibernation;
        private Button3D btnLargeFiles;

        private Label lblTotalJunkNumber;
        private Label lblTotalJunkSub;
        private Panel categoriesContainer;
        private AnimatedLoadingBar loadingBar;
        private Button3D btnScan;
        private Button3D btnClean;
        private Button3D btnAdmin;

        private List<CleanTarget> targets = new List<CleanTarget>();
        private Dictionary<string, CategoryCard3D> categoryCards = new Dictionary<string, CategoryCard3D>();

        private System.Windows.Forms.Timer animTimer;
        private System.Windows.Forms.Timer storagePollTimer;

        private bool isBusy = false;
        private long displayedJunkBytes = 0;
        private long targetJunkBytes = 0;
        private string previewOutPath = null;

        public MainForm(string previewPath = null)
        {
            this.previewOutPath = previewPath;
            this.Text = "Ultimate Deep Cleaner Pro - Milik Sandy (IG: @sandy_gimbal | TikTok: Sandy.gimbal)";
            this.Size = new Size(1040, 705);
            this.MinimumSize = new Size(980, 675);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.ForeColor = Color.White;
            this.Font = NativeWin32.PxFont(13f);
            this.DoubleBuffered = true;

            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            InitTargets();
            BuildModernUI();

            // 1. Live Storage Auto-Reader (Runs immediately + every 2 seconds)
            UpdateLiveStorageAndRam();
            storagePollTimer = new System.Windows.Forms.Timer();
            storagePollTimer.Interval = 2000;
            storagePollTimer.Tick += (s, e) => UpdateLiveStorageAndRam();
            storagePollTimer.Start();

            // 2. 60-FPS Smooth Animation Timer
            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 25;
            animTimer.Tick += OnAnimationTick;
            animTimer.Start();

            // 3. Automatically start Deep Scan on launch
            this.Shown += async (s, e) =>
            {
                await StartDeepScanAsync();
                if (!string.IsNullOrEmpty(previewOutPath))
                {
                    await Task.Delay(350);
                    SaveFormPreview(previewOutPath);
                    Application.Exit();
                }
            };
        }

        private void SaveFormPreview(string path)
        {
            try
            {
                using (Bitmap bmp = new Bitmap(this.Width, this.Height))
                {
                    this.DrawToBitmap(bmp, new Rectangle(0, 0, this.Width, this.Height));
                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            catch { }
        }

        private void InitTargets()
        {
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roamingApp = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // 1. ArcGIS Pro Installer & Local Caches
            var arcgisPaths = new List<string>();
            if (Directory.Exists(docs))
            {
                try
                {
                    foreach (var d in Directory.GetDirectories(docs, "ArcGIS Pro*"))
                    {
                        string sub = Path.Combine(d, "ArcGISPro");
                        arcgisPaths.Add(Directory.Exists(sub) ? sub : d);
                    }
                }
                catch { }
            }
            arcgisPaths.Add(Path.Combine(localApp, @"ESRI\Local Caches"));
            arcgisPaths.Add(Path.Combine(localApp, @"ESRI\ErrorReports"));

            targets.Add(new CleanTarget
            {
                Id = "arcgis",
                Title = "Sisa Installer & Cache Peta ArcGIS Pro (ESRI)",
                Subtitle = "File ekstrak installer (.cab/.msi) di Documents & cache peta di AppData\\Local\\ESRI",
                Paths = arcgisPaths,
                IsChecked = true
            });

            // 2. Developer & Android Studio Caches
            var devPaths = new List<string>
            {
                Path.Combine(userHome, @".gradle\wrapper"),
                Path.Combine(userHome, @".gradle\caches"),
                Path.Combine(userHome, @".gradle\daemon"),
                Path.Combine(userHome, @".gradle\.tmp"),
                Path.Combine(userHome, @".gradle\native"),
                Path.Combine(localApp, "npm-cache"),
                Path.Combine(roamingApp, "npm-cache"),
                Path.Combine(localApp, @"pip\cache"),
                Path.Combine(localApp, @"uv\cache"),
                Path.Combine(localApp, @"Pub\Cache"),
                Path.Combine(localApp, "ms-playwright")
            };

            string googleLocal = Path.Combine(localApp, "Google");
            if (Directory.Exists(googleLocal))
            {
                try
                {
                    foreach (var astudio in Directory.GetDirectories(googleLocal, "AndroidStudio*"))
                    {
                        devPaths.Add(Path.Combine(astudio, "caches"));
                        devPaths.Add(Path.Combine(astudio, "index"));
                        devPaths.Add(Path.Combine(astudio, "log"));
                        devPaths.Add(Path.Combine(astudio, "tmp"));
                        devPaths.Add(Path.Combine(astudio, "local-history"));
                    }
                }
                catch { }
            }

            targets.Add(new CleanTarget
            {
                Id = "dev_caches",
                Title = "Cache Developer (Gradle, Android Studio, NPM, PIP, Flutter)",
                Subtitle = "Membersihkan .gradle, AndroidStudio caches/index, npm-cache, pip/uv, dan Pub cache",
                Paths = devPaths,
                IsChecked = true
            });

            // 3. Browser & App Caches
            var browserPaths = new List<string>();
            AddBrowserProfileCaches(Path.Combine(localApp, @"Google\Chrome\User Data"), browserPaths);
            AddBrowserProfileCaches(Path.Combine(localApp, @"Microsoft\Edge\User Data"), browserPaths);
            browserPaths.Add(Path.Combine(roamingApp, @"TikTok LIVE Studio\gecko_cache"));
            browserPaths.Add(Path.Combine(roamingApp, @"Code\Cache"));
            browserPaths.Add(Path.Combine(roamingApp, @"Code\CachedData"));
            browserPaths.Add(Path.Combine(localApp, "Downloaded Installations"));
            browserPaths.Add(Path.Combine(localApp, "antigravity-updater"));

            targets.Add(new CleanTarget
            {
                Id = "browsers",
                Title = "Cache Browser (Chrome & Edge Semua Profil) + Aplikasi",
                Subtitle = "Cache web, GPUCache, Code Cache, Service Worker, TikTok Studio & VS Code",
                Paths = browserPaths,
                IsChecked = true
            });

            // 4. Windows Temp, Prefetch, CrashDumps & Error Reports
            targets.Add(new CleanTarget
            {
                Id = "win_temp",
                Title = "File Sampah Sistem Windows, Temp, Prefetch & Log",
                Subtitle = "Membersihkan %TEMP%, C:\\Windows\\Temp, Prefetch, CrashDumps, dan laporan WER",
                Paths = new List<string>
                {
                    Path.GetTempPath(),
                    @"C:\Windows\Temp",
                    @"C:\Windows\Prefetch",
                    Path.Combine(localApp, "CrashDumps"),
                    @"C:\ProgramData\Microsoft\Windows\WER",
                    @"C:\Windows\Minidump",
                    @"C:\tmp"
                },
                IsChecked = true
            });

            // 5. Windows Update & Thumbnails
            targets.Add(new CleanTarget
            {
                Id = "win_update",
                Title = "Cache Unduhan Windows Update & Thumbnail Explorer",
                Subtitle = "SoftwareDistribution\\Download, DeliveryOptimization, dan thumbcache_*.db",
                Paths = new List<string>
                {
                    @"C:\Windows\SoftwareDistribution\Download",
                    @"C:\Windows\SoftwareDistribution\DeliveryOptimization"
                },
                IsThumbCache = true,
                IsChecked = true
            });

            // 6. Recycle Bin
            targets.Add(new CleanTarget
            {
                Id = "recycle_bin",
                Title = "Keranjang Sampah (Recycle Bin Semua Partisi)",
                Subtitle = "Mengosongkan seluruh file yang telah dibuang ke Recycle Bin",
                Paths = new List<string> { @"C:\$Recycle.Bin" },
                IsRecycleBin = true,
                IsChecked = true
            });
        }

        private void AddBrowserProfileCaches(string userDataDir, List<string> targetList)
        {
            if (!Directory.Exists(userDataDir)) return;
            try
            {
                string crx = Path.Combine(userDataDir, "component_crx_cache");
                if (Directory.Exists(crx)) targetList.Add(crx);

                foreach (var dir in Directory.GetDirectories(userDataDir))
                {
                    string name = Path.GetFileName(dir);
                    if (name.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("System Profile", StringComparison.OrdinalIgnoreCase))
                    {
                        targetList.Add(Path.Combine(dir, "Cache"));
                        targetList.Add(Path.Combine(dir, "Code Cache"));
                        targetList.Add(Path.Combine(dir, "GPUCache"));
                        targetList.Add(Path.Combine(dir, @"Service Worker\CacheStorage"));
                    }
                }
            }
            catch { }
        }

        private void BuildModernUI()
        {
            // ---------------- TOP BAR ----------------
            Header3DPanel topBar = new Header3DPanel
            {
                Dock = DockStyle.Top,
                Height = 64
            };
            this.Controls.Add(topBar);

            PictureBox picLogo = new PictureBox
            {
                Location = new Point(16, 9),
                Size = new Size(46, 46),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent
            };
            try
            {
                if (this.Icon != null)
                {
                    picLogo.Image = new Icon(this.Icon, 64, 64).ToBitmap();
                }
            }
            catch { }
            topBar.Controls.Add(picLogo);

            Label lblTitle = new Label
            {
                Text = "Ultimate Deep Cleaner Pro  •  Sandy Edition",
                Font = NativeWin32.PxFont(20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                BackColor = Color.Transparent,
                AutoSize = true,
                UseMnemonic = false,
                Location = new Point(70, 8)
            };
            topBar.Controls.Add(lblTitle);

            Label lblSub = new Label
            {
                Text = "👑 Official App Milik Sandy   •   📸 Instagram: @sandy_gimbal   •   🎵 TikTok: Sandy.gimbal",
                Font = NativeWin32.PxFont(12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                BackColor = Color.Transparent,
                AutoSize = true,
                UseMnemonic = false,
                Location = new Point(72, 36)
            };
            topBar.Controls.Add(lblSub);

            bool isAdmin = NativeWin32.IsAdministrator();
            btnAdmin = new Button3D
            {
                Text = isAdmin ? "🛡️ MODE ADMINISTRATOR AKTIF" : "⚠️ Mode Biasa • Klik untuk Mode Admin",
                CustomFont = NativeWin32.PxFont(12f, FontStyle.Bold),
                BaseColor = isAdmin ? Color.FromArgb(22, 101, 52) : Color.FromArgb(194, 65, 12),
                Size = new Size(270, 38),
                Location = new Point(this.ClientSize.Width - 288, 13),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            if (!isAdmin)
            {
                btnAdmin.Click += (s, e) => RestartAsAdmin();
            }
            topBar.Controls.Add(btnAdmin);

            // ---------------- MAIN BODY ----------------
            Panel mainBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16)
            };
            this.Controls.Add(mainBody);
            mainBody.BringToFront();

            // LEFT PANEL: LIVE STORAGE MONITOR & QUICK TOOLS
            Panel leftPanel = new Panel
            {
                Width = 300,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(20, 29, 47)
            };
            leftPanel.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(Color.FromArgb(65, 56, 189, 248), 1.2f))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, leftPanel.Width - 1, leftPanel.Height - 1);
                }
            };
            mainBody.Controls.Add(leftPanel);

            Label lblMonHead = new Label
            {
                Text = "📊 KEADAAN PENYIMPANAN SEKARANG",
                Font = NativeWin32.PxFont(12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                AutoSize = true,
                UseMnemonic = false,
                Location = new Point(16, 10)
            };
            leftPanel.Controls.Add(lblMonHead);

            gaugeC = new Holographic3DGauge
            {
                Location = new Point(10, 28),
                Size = new Size(280, 228)
            };
            leftPanel.Controls.Add(gaugeC);

            // Drive D Cylinder Bar
            miniBarD = new Cylinder3DBar
            {
                Location = new Point(14, 258),
                Size = new Size(272, 46),
                TitleText = "Partisi D:",
                ValueText = "Membaca...",
                FillColor = Color.FromArgb(56, 189, 248)
            };
            leftPanel.Controls.Add(miniBarD);

            // RAM Cylinder Bar
            miniBarRam = new Cylinder3DBar
            {
                Location = new Point(14, 308),
                Size = new Size(272, 46),
                TitleText = "Memori RAM:",
                ValueText = "Membaca...",
                FillColor = Color.FromArgb(168, 85, 247)
            };
            leftPanel.Controls.Add(miniBarRam);

            btnBoostRam = new Button3D
            {
                Text = "⚡ Boost & Legakan RAM (C++ API)",
                CustomFont = NativeWin32.PxFont(12.5f, FontStyle.Bold),
                BaseColor = Color.FromArgb(37, 99, 235),
                Location = new Point(14, 360),
                Size = new Size(272, 36)
            };
            btnBoostRam.Click += (s, e) => BoostRamMemory();
            leftPanel.Controls.Add(btnBoostRam);

            btnHibernation = new Button3D
            {
                Text = "💤 Cek Status Hibernasi...",
                CustomFont = NativeWin32.PxFont(12.5f, FontStyle.Bold),
                BaseColor = Color.FromArgb(185, 28, 28),
                Location = new Point(14, 400),
                Size = new Size(272, 36)
            };
            btnHibernation.Click += (s, e) => ToggleHibernation();
            leftPanel.Controls.Add(btnHibernation);
            UpdateHibernationButton();

            btnLargeFiles = new Button3D
            {
                Text = "🔎 Cari File Raksasa (>100 MB di C:)",
                CustomFont = NativeWin32.PxFont(12.5f, FontStyle.Bold),
                BaseColor = Color.FromArgb(45, 62, 90),
                TextColor = Color.FromArgb(56, 189, 248),
                Location = new Point(14, 440),
                Size = new Size(272, 36)
            };
            btnLargeFiles.Click += (s, e) => OpenLargeFilesDialog();
            leftPanel.Controls.Add(btnLargeFiles);

            // OFFICIAL CREATOR / OWNER CARD (SANDY)
            Panel cardOwner = new Panel
            {
                Location = new Point(14, 482),
                Size = new Size(272, 68),
                BackColor = Color.FromArgb(13, 20, 36)
            };
            cardOwner.Paint += (s, e) =>
            {
                using (Pen goldPen = new Pen(Color.FromArgb(110, 250, 204, 21), 1f))
                {
                    e.Graphics.DrawRectangle(goldPen, 0, 0, cardOwner.Width - 1, cardOwner.Height - 1);
                }
            };
            leftPanel.Controls.Add(cardOwner);

            Label lblOwnerTitle = new Label
            {
                Text = "👑 OFFICIAL OWNER: SANDY",
                Font = NativeWin32.PxFont(11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                UseMnemonic = false,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(272, 20),
                Location = new Point(0, 5)
            };
            cardOwner.Controls.Add(lblOwnerTitle);

            Button3D btnIg = new Button3D
            {
                Text = "📸 @sandy_gimbal",
                CustomFont = NativeWin32.PxFont(11.5f, FontStyle.Bold),
                BaseColor = Color.FromArgb(190, 24, 93),
                ExtrudeDepth = 3,
                Location = new Point(8, 28),
                Size = new Size(125, 32)
            };
            btnIg.Click += (s, e) => { try { Process.Start("https://www.instagram.com/sandy_gimbal"); } catch { } };
            cardOwner.Controls.Add(btnIg);

            Button3D btnTiktok = new Button3D
            {
                Text = "🎵 Sandy.gimbal",
                CustomFont = NativeWin32.PxFont(11.5f, FontStyle.Bold),
                BaseColor = Color.FromArgb(13, 148, 136),
                ExtrudeDepth = 3,
                Location = new Point(139, 28),
                Size = new Size(125, 32)
            };
            btnTiktok.Click += (s, e) => { try { Process.Start("https://www.tiktok.com/@Sandy.gimbal"); } catch { } };
            cardOwner.Controls.Add(btnTiktok);

            // RIGHT PANEL: DEEP CLEANER DASHBOARD
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 0, 0, 0)
            };
            mainBody.Controls.Add(rightPanel);
            rightPanel.BringToFront();

            // Top Summary Banner
            Panel summaryCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Color.FromArgb(22, 32, 54)
            };
            summaryCard.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (LinearGradientBrush lgb = new LinearGradientBrush(
                    new Point(0, 0),
                    new Point(summaryCard.Width, summaryCard.Height),
                    Color.FromArgb(30, 45, 75),
                    Color.FromArgb(18, 27, 46)))
                {
                    g.FillRectangle(lgb, summaryCard.ClientRectangle);
                }
                using (Pen rim = new Pen(Color.FromArgb(90, 56, 189, 248), 1.2f))
                {
                    g.DrawRectangle(rim, 0, 0, summaryCard.Width - 1, summaryCard.Height - 1);
                }
            };
            rightPanel.Controls.Add(summaryCard);

            Label lblJunkTitle = new Label
            {
                Text = "⚡ TOTAL SAMPAH SIAP DIBERSIHKAN:",
                Font = NativeWin32.PxFont(11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                UseMnemonic = false,
                Location = new Point(16, 10),
                AutoSize = true
            };
            summaryCard.Controls.Add(lblJunkTitle);

            lblTotalJunkNumber = new Label
            {
                Text = "0.0 MB",
                Font = NativeWin32.PxFont(28f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                BackColor = Color.Transparent,
                UseMnemonic = false,
                Location = new Point(14, 28),
                AutoSize = true
            };
            summaryCard.Controls.Add(lblTotalJunkNumber);

            lblTotalJunkSub = new Label
            {
                Text = "Menyisir cache tersembunyi sampai ke akar sistem...",
                Font = NativeWin32.PxFont(12.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                BackColor = Color.Transparent,
                UseMnemonic = false,
                Location = new Point(210, 38),
                AutoSize = true
            };
            summaryCard.Controls.Add(lblTotalJunkSub);

            // Bottom Action & Loading Tube Area
            Panel bottomActionPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 98,
                BackColor = Color.FromArgb(15, 23, 42)
            };
            rightPanel.Controls.Add(bottomActionPanel);

            loadingBar = new AnimatedLoadingBar
            {
                Location = new Point(0, 2),
                Size = new Size(rightPanel.ClientSize.Width - 16, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            bottomActionPanel.Controls.Add(loadingBar);

            btnScan = new Button3D
            {
                Text = "🔄 Pindai Ulang",
                CustomFont = NativeWin32.PxFont(14f, FontStyle.Bold),
                BaseColor = Color.FromArgb(51, 65, 85),
                Location = new Point(0, 48),
                Size = new Size(170, 48)
            };
            btnScan.Click += async (s, e) => await StartDeepScanAsync();
            bottomActionPanel.Controls.Add(btnScan);

            btnClean = new Button3D
            {
                Text = "🚀 1-KLIK BERSIHKAN MENDALAM SEKARANG",
                CustomFont = NativeWin32.PxFont(15.5f, FontStyle.Bold),
                BaseColor = Color.FromArgb(16, 185, 129),
                Location = new Point(182, 48),
                Size = new Size(rightPanel.ClientSize.Width - 198, 48),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnClean.Click += (s, e) => StartDeepCleanAsync();
            bottomActionPanel.Controls.Add(btnClean);

            // Center Category Cards Container
            categoriesContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false
            };
            rightPanel.Controls.Add(categoriesContainer);
            categoriesContainer.BringToFront();

            BuildCategoryRows();
            categoriesContainer.Resize += (s, e) => ResizeCategoryRows();
        }

        private void BuildCategoryRows()
        {
            categoriesContainer.Controls.Clear();
            categoryCards.Clear();

            int y = 8;
            int w = Math.Max(500, categoriesContainer.ClientSize.Width);
            foreach (var t in targets)
            {
                CategoryCard3D card = new CategoryCard3D(t)
                {
                    Location = new Point(0, y),
                    Size = new Size(w, 54)
                };
                card.CheckChanged += (s, e) => RecalculateSelectedTotal();
                categoryCards[t.Id] = card;
                categoriesContainer.Controls.Add(card);
                y += 57;
            }
        }

        private void ResizeCategoryRows()
        {
            int w = Math.Max(400, categoriesContainer.ClientSize.Width);
            foreach (var kv in categoryCards)
            {
                kv.Value.Width = w;
            }
        }

        // =====================================================================
        // REAL-TIME AUTO STORAGE & RAM READER (EVERY 2 SECONDS)
        // =====================================================================
        private void UpdateLiveStorageAndRam()
        {
            try
            {
                DriveInfo dc = new DriveInfo("C");
                if (dc.IsReady)
                {
                    double totalGb = dc.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    double freeGb = dc.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    double usedGb = totalGb - freeGb;
                    double pct = (usedGb / totalGb) * 100.0;

                    gaugeC.TargetUsedPercent = pct;
                    gaugeC.FreeText = string.Format("Bebas: {0:0.00} GB", freeGb);
                    gaugeC.UsedText = string.Format("Terpakai: {0:0.0} GB dari {1:0.0} GB", usedGb, totalGb);

                    if (freeGb < 3.0)
                    {
                        gaugeC.HealthBadge = "⚠️ KRITIS (PENUH)";
                        gaugeC.BadgeColor = Color.FromArgb(239, 68, 68);
                    }
                    else if (freeGb < 10.0)
                    {
                        gaugeC.HealthBadge = "PERLU DIBERSIHKAN";
                        gaugeC.BadgeColor = Color.FromArgb(245, 158, 11);
                    }
                    else
                    {
                        gaugeC.HealthBadge = "✓ PENYIMPANAN SEHAT";
                        gaugeC.BadgeColor = Color.FromArgb(16, 185, 129);
                    }
                }

                DriveInfo dd = new DriveInfo("D");
                if (dd.IsReady)
                {
                    double totalD = dd.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    double freeD = dd.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    double usedD = totalD - freeD;
                    float pctD = (float)((usedD / totalD) * 100.0);
                    miniBarD.TitleText = "Partisi D:";
                    miniBarD.ValueText = string.Format("Bebas {0:0.0} GB / {1:0.0} GB", freeD, totalD);
                    miniBarD.Percent = pctD;
                    miniBarD.Invalidate();
                }
            }
            catch { }

            try
            {
                var mem = new NativeWin32.MEMORYSTATUSEX();
                if (NativeWin32.GlobalMemoryStatusEx(mem))
                {
                    double totalRam = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    double availRam = mem.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                    double usedRam = totalRam - availRam;
                    float load = mem.dwMemoryLoad;
                    miniBarRam.TitleText = "Memori RAM:";
                    miniBarRam.ValueText = string.Format("{0:0.0} / {1:0.0} GB ({2}%)", usedRam, totalRam, (int)load);
                    miniBarRam.Percent = load;
                    miniBarRam.Invalidate();
                }
            }
            catch { }
        }

        // =====================================================================
        // 60-FPS HOLOGRAPHIC ANIMATION LOOP
        // =====================================================================
        private void OnAnimationTick(object sender, EventArgs e)
        {
            double diff = gaugeC.TargetUsedPercent - gaugeC.DisplayedUsedPercent;
            if (Math.Abs(diff) > 0.05)
            {
                gaugeC.DisplayedUsedPercent += diff * 0.16;
            }
            else
            {
                gaugeC.DisplayedUsedPercent = gaugeC.TargetUsedPercent;
            }

            gaugeC.Rot3DAngle += (gaugeC.IsLoading || gaugeC.IsCelebrating) ? 0.085f : 0.025f;
            if (gaugeC.IsLoading)
            {
                gaugeC.RadarAngle = (gaugeC.RadarAngle + 8.5f) % 360f;
            }
            if (gaugeC.IsCelebrating)
            {
                gaugeC.StepCelebration();
            }
            gaugeC.Invalidate();

            if (isBusy || loadingBar.Progress > 0f)
            {
                loadingBar.ShimmerOffset += 0.025f;
                if (loadingBar.ShimmerOffset > 1.0f) loadingBar.ShimmerOffset = 0.0f;
                loadingBar.Invalidate();
            }

            if (displayedJunkBytes != targetJunkBytes)
            {
                long step = (long)((targetJunkBytes - displayedJunkBytes) * 0.25);
                if (step == 0) step = targetJunkBytes > displayedJunkBytes ? 1 : -1;
                displayedJunkBytes += step;
                if (Math.Abs(targetJunkBytes - displayedJunkBytes) < 1024 * 50)
                    displayedJunkBytes = targetJunkBytes;

                lblTotalJunkNumber.Text = NativeWin32.FormatBytes(displayedJunkBytes);
            }
        }

        private void RecalculateSelectedTotal()
        {
            long sum = 0;
            int files = 0;
            foreach (var t in targets)
            {
                if (t.IsChecked)
                {
                    sum += t.ScannedBytes;
                    files += t.ScannedFiles;
                }
            }
            targetJunkBytes = sum;
            lblTotalJunkSub.Text = string.Format("•  {0:N0} file sampah siap dibersihkan dengan 1-Klik", files);
        }

        // =====================================================================
        // DEEP SCAN ENGINE (MULTI-THREADED + LOADING ANIMATION + CHIME)
        // =====================================================================
        private async Task StartDeepScanAsync()
        {
            if (isBusy) return;
            isBusy = true;

            ResizeCategoryRows();
            btnScan.Enabled = false;
            btnClean.Enabled = false;
            gaugeC.IsLoading = true;
            gaugeC.LoadingStatus = "Memindai C:...";
            loadingBar.IsIndeterminate = true;
            loadingBar.StatusText = "🔍 Sedang memindai file sampah tersembunyi sampai ke akar sistem...";

            foreach (var kv in categoryCards)
            {
                kv.Value.SizeText = "⏳ Memindai...";
                kv.Value.SizeColor = Color.FromArgb(245, 158, 11);
                kv.Value.Invalidate();
            }

            int totalTargets = targets.Count;
            int completed = 0;

            await Task.Run(() =>
            {
                foreach (var t in targets)
                {
                    this.BeginInvoke((Action)(() =>
                    {
                        loadingBar.StatusText = "🔍 Memindai: " + t.Title + "...";
                    }));

                    long bytes = 0;
                    int files = 0;

                    if (t.IsThumbCache)
                    {
                        string expDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer");
                        if (Directory.Exists(expDir))
                        {
                            try
                            {
                                foreach (var f in Directory.GetFiles(expDir, "*cache_*.db"))
                                {
                                    try { bytes += new FileInfo(f).Length; files++; } catch { }
                                }
                            }
                            catch { }
                        }
                    }

                    foreach (var p in t.Paths)
                    {
                        ScanDirectoryFast(p, ref files, ref bytes);
                    }

                    t.ScannedBytes = bytes;
                    t.ScannedFiles = files;
                    completed++;

                    var captured = t;
                    float pct = (float)completed / totalTargets;
                    this.BeginInvoke((Action)(() =>
                    {
                        loadingBar.IsIndeterminate = false;
                        loadingBar.Progress = pct;
                        if (categoryCards.ContainsKey(captured.Id))
                        {
                            var card = categoryCards[captured.Id];
                            if (captured.ScannedBytes > 0)
                            {
                                card.SizeText = NativeWin32.FormatBytes(captured.ScannedBytes);
                                card.SizeColor = Color.FromArgb(56, 189, 248);
                            }
                            else
                            {
                                card.SizeText = "Bersih (0 MB)";
                                card.SizeColor = Color.FromArgb(100, 116, 139);
                            }
                            card.Invalidate();
                        }
                        RecalculateSelectedTotal();
                    }));

                    Thread.Sleep(90);
                }
            });

            displayedJunkBytes = targetJunkBytes;
            lblTotalJunkNumber.Text = NativeWin32.FormatBytes(targetJunkBytes);
            gaugeC.IsLoading = false;
            loadingBar.IsIndeterminate = false;
            loadingBar.Progress = 1.0f;
            loadingBar.StatusText = string.Format("✅ Pemindaian selesai! Ditemukan {0} sampah yang siap dibersihkan.", NativeWin32.FormatBytes(targetJunkBytes));

            if (string.IsNullOrEmpty(previewOutPath))
            {
                NativeWin32.PlayScanCompleteSound();
            }

            btnScan.Enabled = true;
            btnClean.Enabled = true;
            isBusy = false;
            UpdateLiveStorageAndRam();
        }

        private void ScanDirectoryFast(string path, ref int fileCount, ref long totalBytes)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            Stack<string> dirs = new Stack<string>();
            dirs.Push(path);

            while (dirs.Count > 0)
            {
                string current = dirs.Pop();
                try
                {
                    foreach (string f in Directory.GetFiles(current))
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(f);
                            totalBytes += fi.Length;
                            fileCount++;
                        }
                        catch { }
                    }
                }
                catch { }

                try
                {
                    foreach (string d in Directory.GetDirectories(current))
                    {
                        dirs.Push(d);
                    }
                }
                catch { }
            }
        }

        // =====================================================================
        // DEEP CLEAN ENGINE (1-CLICK + PARTICLE BURST + VICTORY SOUND & POPUP)
        // =====================================================================
        private async void StartDeepCleanAsync()
        {
            if (isBusy) return;

            var selected = targets.Where(t => t.IsChecked).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Pilih minimal satu kategori untuk dibersihkan!", "Informasi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            isBusy = true;
            btnScan.Enabled = false;
            btnClean.Enabled = false;
            gaugeC.IsLoading = true;
            gaugeC.LoadingStatus = "Membersihkan...";
            loadingBar.IsIndeterminate = false;
            loadingBar.Progress = 0.0f;

            var memBefore = new NativeWin32.MEMORYSTATUSEX();
            NativeWin32.GlobalMemoryStatusEx(memBefore);

            long totalFreed = 0;
            int totalDeletedFiles = 0;

            await Task.Run(() =>
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    var t = selected[i];
                    float startPct = (float)i / selected.Count;

                    this.BeginInvoke((Action)(() =>
                    {
                        loadingBar.Progress = startPct;
                        loadingBar.StatusText = "🧹 Membersihkan mendalam: " + t.Title + "...";
                        if (categoryCards.ContainsKey(t.Id))
                        {
                            categoryCards[t.Id].SizeText = "🧹 Menghapus...";
                            categoryCards[t.Id].SizeColor = Color.FromArgb(245, 158, 11);
                            categoryCards[t.Id].Invalidate();
                        }
                    }));

                    long catFreed = 0;
                    int catFiles = 0;

                    if (t.IsRecycleBin)
                    {
                        catFreed = t.ScannedBytes;
                        catFiles = t.ScannedFiles;
                        try
                        {
                            NativeWin32.SHEmptyRecycleBinW(IntPtr.Zero, null, 7);
                        }
                        catch { }
                    }
                    else
                    {
                        if (t.IsThumbCache)
                        {
                            string expDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer");
                            if (Directory.Exists(expDir))
                            {
                                try
                                {
                                    foreach (var f in Directory.GetFiles(expDir, "*cache_*.db"))
                                    {
                                        try
                                        {
                                            long len = new FileInfo(f).Length;
                                            File.SetAttributes(f, FileAttributes.Normal);
                                            File.Delete(f);
                                            catFreed += len;
                                            catFiles++;
                                        }
                                        catch { }
                                    }
                                }
                                catch { }
                            }
                        }

                        foreach (var p in t.Paths)
                        {
                            CleanDirectoryContentsSafe(p, ref catFiles, ref catFreed);
                        }
                    }

                    totalFreed += catFreed;
                    totalDeletedFiles += catFiles;
                    t.ScannedBytes = 0;
                    t.ScannedFiles = 0;

                    var captured = t;
                    float endPct = (float)(i + 1) / selected.Count;
                    this.BeginInvoke((Action)(() =>
                    {
                        loadingBar.Progress = endPct;
                        if (categoryCards.ContainsKey(captured.Id))
                        {
                            categoryCards[captured.Id].SizeText = "✓ Bersih";
                            categoryCards[captured.Id].SizeColor = Color.FromArgb(16, 185, 129);
                            categoryCards[captured.Id].Invalidate();
                        }
                        RecalculateSelectedTotal();
                        UpdateLiveStorageAndRam();
                    }));

                    Thread.Sleep(130);
                }
            });

            // Optimize RAM Working Set via C++ API
            await Task.Run(() => BoostRamSilent());
            await Task.Delay(250);

            var memAfter = new NativeWin32.MEMORYSTATUSEX();
            NativeWin32.GlobalMemoryStatusEx(memAfter);
            long ramFreed = (long)memAfter.ullAvailPhys - (long)memBefore.ullAvailPhys;
            if (ramFreed < 0) ramFreed = 0;

            gaugeC.IsLoading = false;
            loadingBar.Progress = 1.0f;
            loadingBar.StatusText = string.Format("🎉 Pembersihan Berhasil! Membebaskan {0} ruang & mengoptimalkan RAM.", NativeWin32.FormatBytes(totalFreed));

            btnScan.Enabled = true;
            btnClean.Enabled = true;
            isBusy = false;
            UpdateLiveStorageAndRam();

            // Trigger Visual Celebration Burst & Synthesized Victory Fanfare Chime!
            gaugeC.TriggerCelebrationBurst();
            NativeWin32.PlayVictoryFanfareSound();

            string ramStr = ramFreed > 1024 * 1024 ? ("+" + NativeWin32.FormatBytes(ramFreed) + " RAM Lega") : "RAM Optimal & Stabil";
            string freeNowStr = gaugeC.FreeText.Replace("Bebas: ", "") + " (Optimal)";

            using (VictoryCelebrationForm victoryDlg = new VictoryCelebrationForm(
                NativeWin32.FormatBytes(totalFreed),
                totalDeletedFiles,
                ramStr,
                freeNowStr))
            {
                victoryDlg.ShowDialog(this);
            }
        }

        private void CleanDirectoryContentsSafe(string rootPath, ref int filesDeleted, ref long bytesFreed)
        {
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) return;

            try
            {
                foreach (string file in Directory.GetFiles(rootPath))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        long sz = fi.Length;
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        bytesFreed += sz;
                        filesDeleted++;
                    }
                    catch { }
                }
            }
            catch { }

            try
            {
                foreach (string subDir in Directory.GetDirectories(rootPath))
                {
                    CleanDirectoryContentsSafe(subDir, ref filesDeleted, ref bytesFreed);
                    try
                    {
                        Directory.Delete(subDir, true);
                    }
                    catch { }
                }
            }
            catch { }
        }

        // =====================================================================
        // NATIVE C++ RAM WORKING SET TRIMMER (psapi.dll -> EmptyWorkingSet)
        // =====================================================================
        private int BoostRamSilent()
        {
            int count = 0;
            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        NativeWin32.EmptyWorkingSet(proc.Handle);
                        count++;
                    }
                }
                catch { }
            }
            return count;
        }

        private async void BoostRamMemory()
        {
            btnBoostRam.Enabled = false;
            btnBoostRam.Text = "⏳ Melegakan RAM...";
            btnBoostRam.Invalidate();

            var memBefore = new NativeWin32.MEMORYSTATUSEX();
            NativeWin32.GlobalMemoryStatusEx(memBefore);

            await Task.Run(() => BoostRamSilent());
            await Task.Delay(350);

            var memAfter = new NativeWin32.MEMORYSTATUSEX();
            NativeWin32.GlobalMemoryStatusEx(memAfter);

            long freedBytes = (long)memAfter.ullAvailPhys - (long)memBefore.ullAvailPhys;
            if (freedBytes < 0) freedBytes = 0;

            UpdateLiveStorageAndRam();
            gaugeC.TriggerCelebrationBurst();
            NativeWin32.PlayScanCompleteSound();

            btnBoostRam.Text = string.Format("✓ RAM Lega (+{0})", NativeWin32.FormatBytes(freedBytes));
            btnBoostRam.Invalidate();
            await Task.Delay(2000);
            btnBoostRam.Text = "⚡ Boost & Legakan RAM (C++ API)";
            btnBoostRam.Enabled = true;
            btnBoostRam.Invalidate();
        }

        // =====================================================================
        // HIBERNATION MANAGER (hiberfil.sys ~3.14 GB)
        // =====================================================================
        private void UpdateHibernationButton()
        {
            bool exists = File.Exists(@"C:\hiberfil.sys");
            if (exists)
            {
                long sz = 0;
                try { sz = new FileInfo(@"C:\hiberfil.sys").Length; } catch { }
                string szStr = sz > 0 ? NativeWin32.FormatBytes(sz) : "~3.1 GB";
                btnHibernation.Text = string.Format("💤 Matikan Hibernasi (Hemat {0})", szStr);
                btnHibernation.BaseColor = Color.FromArgb(185, 28, 28);
            }
            else
            {
                btnHibernation.Text = "✓ Hibernasi Nonaktif (Hemat 3.1 GB)";
                btnHibernation.BaseColor = Color.FromArgb(20, 83, 45);
            }
            btnHibernation.Invalidate();
        }

        private void ToggleHibernation()
        {
            if (!NativeWin32.IsAdministrator())
            {
                var ask = MessageBox.Show(
                    "Menonaktifkan atau mengaktifkan file hibernasi (C:\\hiberfil.sys) memerlukan Mode Administrator.\n\nIngin membuka ulang aplikasi sebagai Administrator sekarang?",
                    "Diperlukan Izin Administrator",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (ask == DialogResult.Yes) RestartAsAdmin();
                return;
            }

            bool exists = File.Exists(@"C:\hiberfil.sys");
            string args = exists ? "/h off" : "/h on";
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("powercfg.exe", args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process p = Process.Start(psi);
                p.WaitForExit(5000);
                UpdateHibernationButton();
                UpdateLiveStorageAndRam();
                gaugeC.TriggerCelebrationBurst();
                NativeWin32.PlayVictoryFanfareSound();
                MessageBox.Show(
                    exists ? "Hibernasi berhasil dimatikan! File C:\\hiberfil.sys (~3.1 GB) telah dihapus dari Drive C." : "Hibernasi telah diaktifkan kembali.",
                    "Sukses",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengatur hibernasi: " + ex.Message);
            }
        }

        private void RestartAsAdmin()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
                Application.Exit();
            }
            catch { }
        }

        // =====================================================================
        // LARGE FILES FINDER MODAL (> 100 MB)
        // =====================================================================
        private void OpenLargeFilesDialog()
        {
            Form dlg = new Form
            {
                Text = "🔎 Pencari File Raksasa (> 100 MB) di Partisi C:",
                Size = new Size(780, 500),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Font = NativeWin32.PxFont(12.5f)
            };

            Label lblInfo = new Label
            {
                Text = "⏳ Sedang memindai file berukuran > 100 MB di folder pengguna C:\\Users...",
                Dock = DockStyle.Top,
                Height = 36,
                UseMnemonic = false,
                Padding = new Padding(12, 8, 12, 0),
                ForeColor = Color.FromArgb(56, 189, 248),
                Font = NativeWin32.PxFont(13f, FontStyle.Bold)
            };
            dlg.Controls.Add(lblInfo);

            ListView lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(24, 33, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            lv.Columns.Add("Nama File", 220);
            lv.Columns.Add("Ukuran", 100);
            lv.Columns.Add("Lokasi Lengkap", 420);
            dlg.Controls.Add(lv);
            lv.BringToFront();

            Panel bottom = new Panel { Dock = DockStyle.Bottom, Height = 54, Padding = new Padding(10) };
            dlg.Controls.Add(bottom);

            Button3D btnOpenLoc = new Button3D
            {
                Text = "📂 Buka Lokasi di Explorer",
                Size = new Size(200, 38),
                Location = new Point(10, 8),
                BaseColor = Color.FromArgb(37, 99, 235)
            };
            btnOpenLoc.Click += (s, e) =>
            {
                if (lv.SelectedItems.Count > 0)
                {
                    string fp = lv.SelectedItems[0].SubItems[2].Text;
                    if (File.Exists(fp)) Process.Start("explorer.exe", "/select,\"" + fp + "\"");
                }
            };
            bottom.Controls.Add(btnOpenLoc);

            Button3D btnDelFile = new Button3D
            {
                Text = "🗑️ Hapus File Terpilih",
                Size = new Size(180, 38),
                Location = new Point(220, 8),
                BaseColor = Color.FromArgb(220, 38, 38)
            };
            btnDelFile.Click += (s, e) =>
            {
                if (lv.SelectedItems.Count == 0) return;
                string fp = lv.SelectedItems[0].SubItems[2].Text;
                if (MessageBox.Show("Hapus permanen file ini?\n\n" + fp, "Konfirmasi Hapus", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        File.SetAttributes(fp, FileAttributes.Normal);
                        File.Delete(fp);
                        lv.SelectedItems[0].Remove();
                        UpdateLiveStorageAndRam();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Gagal menghapus file: " + ex.Message);
                    }
                }
            };
            bottom.Controls.Add(btnDelFile);

            dlg.Shown += async (s, e) =>
            {
                string userRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var found = await Task.Run(() =>
                {
                    var list = new List<FileInfo>();
                    Stack<string> stack = new Stack<string>();
                    stack.Push(userRoot);
                    while (stack.Count > 0)
                    {
                        string dir = stack.Pop();
                        try
                        {
                            foreach (var f in Directory.GetFiles(dir))
                            {
                                try
                                {
                                    FileInfo fi = new FileInfo(f);
                                    if (fi.Length >= 100L * 1024L * 1024L) list.Add(fi);
                                }
                                catch { }
                            }
                            foreach (var sub in Directory.GetDirectories(dir))
                            {
                                stack.Push(sub);
                            }
                        }
                        catch { }
                    }
                    return list.OrderByDescending(x => x.Length).Take(50).ToList();
                });

                lblInfo.Text = string.Format("✅ Ditemukan {0} file berukuran di atas 100 MB:", found.Count);
                foreach (var fi in found)
                {
                    var item = new ListViewItem(fi.Name);
                    item.SubItems.Add(NativeWin32.FormatBytes(fi.Length));
                    item.SubItems.Add(fi.FullName);
                    lv.Items.Add(item);
                }
            };

            dlg.ShowDialog(this);
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string previewPath = null;
            if (args != null && args.Length >= 2 && args[0] == "--preview")
            {
                previewPath = args[1];
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(previewPath));
        }
    }
}
