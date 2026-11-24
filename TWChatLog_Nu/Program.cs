using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TWChatLog_Nu.Properties;

namespace TWChatLog_Nu
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var savedSettings = OverlaySettingsStore.Load();
            var overlay = new OverlayForm(savedSettings);
            var control = new ControlForm(overlay);
            overlay.Show();
            Application.Run(control);
        }
    }

    /// <summary>
    /// JSON 직렬화용 설정 모델 (.NET Framework 4.7.2 DataContractJsonSerializer 호환)
    /// </summary>
    [DataContract]
    public sealed class OverlaySettingsData
    {
        [DataMember] public string WatchFolder { get; set; } = @"C:\Nexon\TalesWeaver\ChatLog";
        [DataMember] public string TargetPhrase { get; set; } = "누군가";
        [DataMember] public bool WatchEnabled { get; set; } = true;
        [DataMember] public int X { get; set; } = 1400;
        [DataMember] public int Y { get; set; } = 80;
        [DataMember] public int Width { get; set; } = 340;
        [DataMember] public int Height { get; set; } = 96;
        [DataMember] public double Opacity { get; set; } = 0.75;
        [DataMember] public float FontSize { get; set; } = 14f;
        [DataMember] public bool ShowBorder { get; set; } = true;
        [DataMember] public int BorderColorArgb { get; set; } = Color.FromArgb(220, 120, 200, 255).ToArgb();
        [DataMember] public bool AlertEnabled { get; set; } = false;
        [DataMember] public int AlertMinutes { get; set; } = 10;
    }

    /// <summary>
    /// 설정값 저장/로드 헬퍼
    /// </summary>
    public static class OverlaySettingsStore
    {
        private static string SettingsFilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TWChatLog_Nu_settings.json"); }
        }

        public static OverlaySettingsData Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                    return new OverlaySettingsData();

                byte[] bytes = File.ReadAllBytes(SettingsFilePath);
                using (var ms = new MemoryStream(bytes))
                {
                    var serializer = new DataContractJsonSerializer(typeof(OverlaySettingsData));
                    var data = serializer.ReadObject(ms) as OverlaySettingsData;
                    return data ?? new OverlaySettingsData();
                }
            }
            catch
            {
                return new OverlaySettingsData();
            }
        }

        public static void Save(OverlaySettingsData data)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(typeof(OverlaySettingsData));
                    serializer.WriteObject(ms, data);
                    File.WriteAllBytes(SettingsFilePath, ms.ToArray());
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// 리소스 내장 Notification.mp3 추출 및 재생 헬퍼
    /// </summary>
    public static class NotificationSoundPlayer
    {
        private static object _player;
        private static MethodInfo _openMethod;
        private static MethodInfo _playMethod;
        private static MethodInfo _stopMethod;
        private static string _extractedMp3Path;

        private static string ExtractNotificationMp3()
        {
            if (!string.IsNullOrEmpty(_extractedMp3Path) && File.Exists(_extractedMp3Path))
                return _extractedMp3Path;

            byte[] mp3Bytes = Resources.Notification;
            if (mp3Bytes == null || mp3Bytes.Length == 0)
                return null;

            string tempPath = Path.Combine(Path.GetTempPath(), "TWChatLog_Nu_Notification.mp3");
            try
            {
                if (!File.Exists(tempPath) || new FileInfo(tempPath).Length != mp3Bytes.Length)
                {
                    File.WriteAllBytes(tempPath, mp3Bytes);
                }
                _extractedMp3Path = tempPath;
                return _extractedMp3Path;
            }
            catch
            {
                return null;
            }
        }

        public static bool Play()
        {
            string mp3Path = ExtractNotificationMp3();
            if (string.IsNullOrEmpty(mp3Path) || !File.Exists(mp3Path))
                return false;

            try
            {
                EnsureMediaPlayer();
                if (_player == null || _openMethod == null || _playMethod == null)
                    return false;

                if (_stopMethod != null)
                    _stopMethod.Invoke(_player, null);

                var uri = new Uri(mp3Path, UriKind.Absolute);
                _openMethod.Invoke(_player, new object[] { uri });
                _playMethod.Invoke(_player, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureMediaPlayer()
        {
            if (_player != null) return;

            Assembly presentationCore = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, "PresentationCore", StringComparison.OrdinalIgnoreCase));

            if (presentationCore == null)
            {
                try
                {
                    presentationCore = Assembly.Load("PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                }
                catch { }
            }

            if (presentationCore == null) return;

            Type mediaPlayerType = presentationCore.GetType("System.Windows.Media.MediaPlayer");
            if (mediaPlayerType == null) return;

            _player = Activator.CreateInstance(mediaPlayerType);
            _openMethod = mediaPlayerType.GetMethod("Open", new[] { typeof(Uri) });
            _playMethod = mediaPlayerType.GetMethod("Play", Type.EmptyTypes);
            _stopMethod = mediaPlayerType.GetMethod("Stop", Type.EmptyTypes);
        }
    }

    /// <summary>
    /// 최상단 반투명 마우스 투과 오버레이 창 (.NET Framework 4.7.2)
    /// </summary>
    public class OverlayForm : Form
    {
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOACTIVATE = 0x0010;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private readonly Timer _clockTimer;
        private readonly Timer _pollTimer;
        private FileSystemWatcher _watcher;

        private string _lastMatchTimeText = "미발생";
        private string _lastMatchedOccurrenceKey = null;
        private DateTime? _alarmTargetTime = null;
        private string _lastMatchedFile = "";
        private DateTime _lastWriteUtc = DateTime.MinValue;
        private long _lastFileLength = -1;

        public string WatchFolder { get; private set; }
        public string TargetPhrase { get; private set; }
        public bool WatchEnabled { get; private set; }
        public int ConfiguredWidth { get; private set; }
        public int ConfiguredHeight { get; private set; }
        public double OverlayOpacity { get; private set; }
        public float TextFontSize { get; private set; }
        public bool ShowBorder { get; private set; }
        public Color BorderColor { get; private set; }
        public bool AlertEnabled { get; private set; }
        public int AlertMinutes { get; private set; }

        public event Action<string> StatusChanged;
        public event Action<bool> AlertStateChanged;

        public OverlayForm(OverlaySettingsData initial = null)
        {
            var cfg = initial ?? new OverlaySettingsData();

            WatchFolder = string.IsNullOrWhiteSpace(cfg.WatchFolder)
                ? @"C:\Nexon\TalesWeaver\ChatLog"
                : cfg.WatchFolder;
            TargetPhrase = string.IsNullOrWhiteSpace(cfg.TargetPhrase)
                ? "누군가"
                : cfg.TargetPhrase;
            WatchEnabled = cfg.WatchEnabled;
            ConfiguredWidth = ClampInt(cfg.Width, 160, 2500);
            ConfiguredHeight = ClampInt(cfg.Height, 80, 1200);
            OverlayOpacity = ClampDouble(cfg.Opacity, 0.1, 1.0);
            TextFontSize = ClampFloat(cfg.FontSize, 8f, 64f);
            ShowBorder = cfg.ShowBorder;
            BorderColor = cfg.BorderColorArgb == 0
                ? Color.FromArgb(220, 120, 200, 255)
                : Color.FromArgb(cfg.BorderColorArgb);
            AlertEnabled = WatchEnabled && cfg.AlertEnabled;
            AlertMinutes = ClampInt(cfg.AlertMinutes, 1, 180);

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(20, 22, 28);
            Opacity = OverlayOpacity;
            Size = new Size(ConfiguredWidth, ConfiguredHeight);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(cfg.X, cfg.Y);

            _clockTimer = new Timer { Interval = 250 };
            _clockTimer.Tick += (s, e) =>
            {
                EnsureTopMost();
                CheckAlertTimer();
                Invalidate();
            };
            _clockTimer.Start();

            _pollTimer = new Timer { Interval = 500 };
            _pollTimer.Tick += (s, e) =>
            {
                if (WatchEnabled)
                    CheckLatestFileIfChanged();
            };
            _pollTimer.Start();

            ApplyOverlayBounds();
            ApplyWatchState(true);
        }

        public static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static double ClampDouble(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static float ClampFloat(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            EnsureTopMost();
        }

        private void EnsureTopMost()
        {
            if (IsHandleCreated && !IsDisposed)
            {
                SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
        }

        private int GetCompactHeight()
        {
            int pad = 8;
            using (var font = new Font("맑은 고딕", TextFontSize, FontStyle.Bold))
            {
                int lineH = (int)Math.Ceiling(font.GetHeight()) + 6;
                return Math.Max(32, pad * 2 + lineH);
            }
        }

        private void ApplyOverlayBounds()
        {
            int targetH = WatchEnabled ? ConfiguredHeight : GetCompactHeight();
            Size = new Size(ConfiguredWidth, targetH);
        }

        public void SaveCurrentSettings()
        {
            OverlaySettingsStore.Save(new OverlaySettingsData
            {
                WatchFolder = WatchFolder,
                TargetPhrase = TargetPhrase,
                WatchEnabled = WatchEnabled,
                X = Left,
                Y = Top,
                Width = ConfiguredWidth,
                Height = ConfiguredHeight,
                Opacity = OverlayOpacity,
                FontSize = TextFontSize,
                ShowBorder = ShowBorder,
                BorderColorArgb = BorderColor.ToArgb(),
                AlertEnabled = WatchEnabled && AlertEnabled,
                AlertMinutes = AlertMinutes
            });
        }

        public void UpdateOverlaySettings(
            string folder,
            string targetPhrase,
            bool watchEnabled,
            int x,
            int y,
            int width,
            int height,
            double opacity,
            float fontSize,
            bool showBorder,
            Color borderColor,
            bool alertEnabled,
            int alertMinutes)
        {
            bool folderChanged = !string.Equals(WatchFolder, folder, StringComparison.OrdinalIgnoreCase);
            bool phraseChanged = !string.Equals(TargetPhrase, targetPhrase, StringComparison.Ordinal);
            bool watchChanged = WatchEnabled != watchEnabled;
            bool effectiveAlertEnabled = watchEnabled && alertEnabled;
            bool alertConfigChanged = (AlertEnabled != effectiveAlertEnabled) || (AlertMinutes != alertMinutes);

            WatchFolder = folder;
            TargetPhrase = targetPhrase;
            WatchEnabled = watchEnabled;
            ConfiguredWidth = ClampInt(width, 160, 2500);
            ConfiguredHeight = ClampInt(height, 80, 1200);
            OverlayOpacity = ClampDouble(opacity, 0.1, 1.0);
            TextFontSize = ClampFloat(fontSize, 8f, 64f);
            ShowBorder = showBorder;
            BorderColor = borderColor;
            AlertEnabled = effectiveAlertEnabled;
            AlertMinutes = ClampInt(alertMinutes, 1, 180);

            Location = new Point(x, y);
            Opacity = OverlayOpacity;
            ApplyOverlayBounds();

            if (folderChanged)
            {
                ResetFileState();
                _lastMatchedOccurrenceKey = null;
                _alarmTargetTime = null;
            }

            if (!WatchEnabled)
            {
                StopWatcher();
                _alarmTargetTime = null;
                PublishStatus();
            }
            else if (watchChanged || folderChanged || phraseChanged)
            {
                ApplyWatchState(true);
            }
            else if (alertConfigChanged)
            {
                RecalculateAlarmForCurrentOccurrence();
                PublishStatus();
            }

            SaveCurrentSettings();
            EnsureTopMost();
            Invalidate();
        }

        private void ApplyWatchState(bool forceScan)
        {
            if (!WatchEnabled)
            {
                StopWatcher();
                _alarmTargetTime = null;
                PublishStatus();
                return;
            }

            SetupWatcher();
            if (forceScan)
            {
                ScanLatestLogFile(true);
            }
            else
            {
                RecalculateAlarmForCurrentOccurrence();
                PublishStatus();
            }
        }

        private void StopWatcher()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        private void RecalculateAlarmForCurrentOccurrence()
        {
            _alarmTargetTime = null;

            if (!WatchEnabled || !AlertEnabled || string.IsNullOrEmpty(_lastMatchedOccurrenceKey))
                return;

            TimeSpan tod;
            if (TryParseOccurrenceTimeOfDay(_lastMatchTimeText, out tod))
            {
                DateTime now = DateTime.Now;
                DateTime baseDt = now.Date + tod;
                if (baseDt > now.AddHours(12))
                    baseDt = baseDt.AddDays(-1);

                DateTime target = baseDt.AddMinutes(AlertMinutes);
                if (target > now)
                {
                    _alarmTargetTime = target;
                }
            }
        }

        private void CheckAlertTimer()
        {
            if (!WatchEnabled || !AlertEnabled || !_alarmTargetTime.HasValue)
                return;

            if (DateTime.Now >= _alarmTargetTime.Value)
            {
                _alarmTargetTime = null;
                AlertEnabled = false;
                PlayAlertSound();
                SaveCurrentSettings();
                var handler = AlertStateChanged;
                if (handler != null) handler(false);
                PublishStatus();
            }
        }

        private static void PlayAlertSound()
        {
            NotificationSoundPlayer.Play();
        }

        private void ResetFileState()
        {
            _lastMatchedFile = "";
            _lastWriteUtc = DateTime.MinValue;
            _lastFileLength = -1;
        }

        private void SetupWatcher()
        {
            StopWatcher();

            if (!Directory.Exists(WatchFolder))
            {
                RaiseStatus(string.Format("폴더 없음: {0}", WatchFolder));
                return;
            }

            _watcher = new FileSystemWatcher(WatchFolder, "TWChatLog_*.html")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Changed += (s, e) => ScheduleScan(e.FullPath);
            _watcher.Created += (s, e) => ScheduleScan(e.FullPath);
            _watcher.Renamed += (s, e) => ScheduleScan(e.FullPath);
        }

        private void ScheduleScan(string filePath)
        {
            if (IsDisposed || !WatchEnabled) return;
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke((MethodInvoker)delegate { ScanFile(filePath, false); });
                }
                else
                {
                    ScanFile(filePath, false);
                }
            }
            catch { }
        }

        private void CheckLatestFileIfChanged()
        {
            if (!WatchEnabled) return;

            string latest = GetLatestLogFile();
            if (latest == null) return;

            try
            {
                var info = new FileInfo(latest);
                info.Refresh();
                DateTime writeUtc = info.LastWriteTimeUtc;
                long length = info.Length;

                if (!string.Equals(latest, _lastMatchedFile, StringComparison.OrdinalIgnoreCase)
                    || writeUtc != _lastWriteUtc
                    || length != _lastFileLength)
                {
                    ScanFile(latest, false);
                }
            }
            catch { }
        }

        public void ScanLatestLogFile(bool force = true)
        {
            if (!WatchEnabled)
            {
                PublishStatus();
                Invalidate();
                return;
            }

            string latest = GetLatestLogFile();
            if (latest == null)
            {
                _lastMatchTimeText = "로그 파일 없음";
                _lastMatchedOccurrenceKey = null;
                _alarmTargetTime = null;
                RaiseStatus(string.Format("TWChatLog_*.html 파일을 찾을 수 없습니다.\n경로: {0}", WatchFolder));
                Invalidate();
                return;
            }
            ScanFile(latest, force);
        }

        private string GetLatestLogFile()
        {
            if (!Directory.Exists(WatchFolder)) return null;
            return Directory.GetFiles(WatchFolder, "TWChatLog_*.html")
                            .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                            .ThenByDescending(f => f, StringComparer.OrdinalIgnoreCase)
                            .FirstOrDefault();
        }

        private void ScanFile(string filePath, bool force)
        {
            if (!WatchEnabled) return;

            try
            {
                if (!File.Exists(filePath)) return;

                var info = new FileInfo(filePath);
                info.Refresh();
                DateTime writeUtc = info.LastWriteTimeUtc;
                long length = info.Length;

                if (!force
                    && string.Equals(filePath, _lastMatchedFile, StringComparison.OrdinalIgnoreCase)
                    && writeUtc == _lastWriteUtc
                    && length == _lastFileLength)
                {
                    return;
                }

                string content = ReadAllTextShared(filePath);
                _lastMatchedFile = filePath;
                _lastWriteUtc = writeUtc;
                _lastFileLength = length;

                string formattedTime;
                int matchCount;
                if (ExtractLastOccurrence(content, TargetPhrase, out formattedTime, out matchCount))
                {
                    _lastMatchTimeText = formattedTime;
                    string occurrenceKey = string.Format("{0}|{1}|{2}", Path.GetFileName(filePath), formattedTime, matchCount);

                    if (!string.Equals(_lastMatchedOccurrenceKey, occurrenceKey, StringComparison.Ordinal))
                    {
                        _lastMatchedOccurrenceKey = occurrenceKey;
                        _alarmTargetTime = null;
                        RecalculateAlarmForCurrentOccurrence();
                    }
                }
                else
                {
                    _lastMatchTimeText = "미발생";
                    _lastMatchedOccurrenceKey = null;
                    _alarmTargetTime = null;
                }

                PublishStatus();
                Invalidate();
            }
            catch (Exception ex)
            {
                RaiseStatus(string.Format("읽기 오류: {0}", ex.Message));
            }
        }

        private void RaiseStatus(string msg)
        {
            var handler = StatusChanged;
            if (handler != null) handler(msg);
        }

        private void PublishStatus()
        {
            if (!WatchEnabled)
            {
                RaiseStatus("로그 감시: 꺼짐 (체크 시 감시 및 알림 사용 가능)");
                return;
            }

            string filePart = string.IsNullOrEmpty(_lastMatchedFile)
                ? string.Format("경로: {0}", WatchFolder)
                : string.Format("파일: {0}", Path.GetFileName(_lastMatchedFile));

            string alarmPart = !AlertEnabled
                ? "알림: 꺼짐"
                : _alarmTargetTime.HasValue
                    ? string.Format("알림예정: {0:HH시 mm분 ss초} ({1}분뒤)", _alarmTargetTime.Value, AlertMinutes)
                    : string.Format("알림: {0}분뒤 (대기)", AlertMinutes);

            RaiseStatus(string.Format(
                "{0}\n업데이트: {1:HH:mm:ss} | 마지막: {2} | {3}",
                filePart, DateTime.Now, _lastMatchTimeText, alarmPart));
        }

        private static bool TryParseOccurrenceTimeOfDay(string formattedTime, out TimeSpan tod)
        {
            tod = TimeSpan.Zero;
            var m = Regex.Match(formattedTime, @"^(?<h>\d{2})시\s*(?<m>\d{2})분\s*(?<s>\d{2})초$");
            if (!m.Success) return false;

            int h = int.Parse(m.Groups["h"].Value);
            int min = int.Parse(m.Groups["m"].Value);
            int s = int.Parse(m.Groups["s"].Value);
            if (h < 0 || h > 23 || min < 0 || min > 59 || s < 0 || s > 59) return false;

            tod = new TimeSpan(h, min, s);
            return true;
        }

        private static string ReadAllTextShared(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                Encoding enc;
                try
                {
                    enc = Encoding.GetEncoding(949);
                }
                catch
                {
                    enc = Encoding.Default;
                }
                using (var sr = new StreamReader(fs, enc, true))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private static bool ExtractLastOccurrence(string html, string phrase, out string lastFormatted, out int matchCount)
        {
            lastFormatted = null;
            matchCount = 0;

            if (string.IsNullOrWhiteSpace(phrase) || string.IsNullOrEmpty(html)) return false;

            var timeRegex = new Regex(@"\[\s*(?<h>\d{1,2})\s*시\s*(?<m>\d{1,2})\s*분\s*(?<s>\d{1,2})\s*초\s*\]");
            var matches = timeRegex.Matches(html);
            if (matches.Count == 0) return false;

            for (int i = 0; i < matches.Count; i++)
            {
                int startIdx = matches[i].Index;
                int endIdx = (i + 1 < matches.Count) ? matches[i + 1].Index : html.Length;
                string segmentHtml = html.Substring(startIdx, endIdx - startIdx);

                string plainText = Regex.Replace(segmentHtml, @"<[^>]+>", " ");
                plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

                if (plainText.IndexOf(phrase, StringComparison.Ordinal) >= 0)
                {
                    matchCount++;
                    int h = int.Parse(matches[i].Groups["h"].Value);
                    int m = int.Parse(matches[i].Groups["m"].Value);
                    int s = int.Parse(matches[i].Groups["s"].Value);
                    lastFormatted = string.Format("{0:D2}시 {1:D2}분 {2:D2}초", h, m, s);
                }
            }

            return lastFormatted != null;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (ShowBorder)
            {
                using (var borderPen = new Pen(BorderColor, 2f))
                {
                    var rect = new RectangleF(1f, 1f, Width - 2f, Height - 2f);
                    g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }

            using (var font = new Font("맑은 고딕", TextFontSize, FontStyle.Bold))
            using (var topBrush = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
            using (var bottomBrush = new SolidBrush(Color.FromArgb(250, 255, 130, 255)))
            using (var alarmBrush = new SolidBrush(Color.FromArgb(250, 255, 220, 110)))
            using (var leftSf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            using (var rightSf = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                string nowValue = string.Format("{0:HH시 mm분 ss초}", DateTime.Now);

                if (!WatchEnabled)
                {
                    int padCompact = 8;
                    var singleRect = new RectangleF(
                        padCompact,
                        padCompact,
                        Math.Max(10, Width - padCompact * 2),
                        Math.Max(10, Height - padCompact * 2));

                    g.DrawString("현재시간 :", font, topBrush, singleRect, leftSf);
                    g.DrawString(nowValue, font, topBrush, singleRect, rightSf);
                    return;
                }

                int pad = 10;
                int innerW = Math.Max(10, Width - pad * 2);
                int halfH = Math.Max(10, (Height - pad * 2) / 2);

                var topRect = new RectangleF(pad, pad, innerW, halfH);
                var bottomRect = new RectangleF(pad, pad + halfH, innerW, halfH);

                using (var divPen = new Pen(Color.FromArgb(90, 255, 255, 255), 1f))
                {
                    g.DrawLine(divPen, pad, pad + halfH, Width - pad, pad + halfH);
                }

                // 상단: 좌측 "현재시간 :", 우측 현재 시간값
                g.DrawString("현재시간 :", font, topBrush, topRect, leftSf);
                g.DrawString(nowValue, font, topBrush, topRect, rightSf);

                // 하단: 로그 감시 동작 중에만 출현시간(및 알림 설정 시각) 표시
                if (AlertEnabled && _alarmTargetTime.HasValue)
                {
                    float subSize = Math.Max(8f, TextFontSize * 0.8f);
                    using (var subFont = new Font("맑은 고딕", subSize, FontStyle.Bold))
                    {
                        float lineH = bottomRect.Height / 2f;
                        var occRect = new RectangleF(bottomRect.X, bottomRect.Y, bottomRect.Width, lineH);
                        var almRect = new RectangleF(bottomRect.X, bottomRect.Y + lineH, bottomRect.Width, lineH);

                        g.DrawString("누 출현시간 :", subFont, bottomBrush, occRect, leftSf);
                        g.DrawString(_lastMatchTimeText, subFont, bottomBrush, occRect, rightSf);

                        g.DrawString("알림 시각 :", subFont, alarmBrush, almRect, leftSf);
                        g.DrawString(string.Format("{0:HH시 mm분 ss초}", _alarmTargetTime.Value), subFont, alarmBrush, almRect, rightSf);
                    }
                }
                else
                {
                    g.DrawString("누 출현시간 :", font, bottomBrush, bottomRect, leftSf);
                    g.DrawString(_lastMatchTimeText, font, bottomBrush, bottomRect, rightSf);
                }
            }
        }
    }
}