using System;
using System.Drawing;
using System.Windows.Forms;
using TWChatLog_Nu.Properties;

namespace TWChatLog_Nu
{
    /// <summary>
    /// 기능별 GroupBox로 분리된 설정/조정 창 (동작 로직)
    /// </summary>
    public partial class ControlForm : Form
    {
        private readonly OverlayForm _overlay;
        private Color _selectedBorderColor;
        private bool _syncingUi;

        public ControlForm(OverlayForm overlay)
        {
            _overlay = overlay;
            _selectedBorderColor = _overlay.BorderColor;

            InitializeComponent();
            LoadWindowIcon();
            BindInitialValues();
            WireEvents();
            UpdateAlertControlsEnabledState();
        }

        private void BindInitialValues()
        {
            _txtFolder.Text = _overlay.WatchFolder;
            _txtTargetText.Text = _overlay.TargetPhrase;
            _chkWatchEnabled.Checked = _overlay.WatchEnabled;

            _numX.Value = OverlayForm.ClampInt(_overlay.Left, (int)_numX.Minimum, (int)_numX.Maximum);
            _numY.Value = OverlayForm.ClampInt(_overlay.Top, (int)_numY.Minimum, (int)_numY.Maximum);
            _numWidth.Value = OverlayForm.ClampInt(_overlay.ConfiguredWidth, (int)_numWidth.Minimum, (int)_numWidth.Maximum);
            _numHeight.Value = OverlayForm.ClampInt(_overlay.ConfiguredHeight, (int)_numHeight.Minimum, (int)_numHeight.Maximum);

            int opacityPct = OverlayForm.ClampInt((int)(_overlay.OverlayOpacity * 100), _trkOpacity.Minimum, _trkOpacity.Maximum);
            _trkOpacity.Value = opacityPct;
            _lblOpacityVal.Text = string.Format("{0}%", opacityPct);

            _chkBorder.Checked = _overlay.ShowBorder;
            UpdateBorderColorButtonStyle();

            _numFontSize.Value = OverlayForm.ClampInt((int)_overlay.TextFontSize, (int)_numFontSize.Minimum, (int)_numFontSize.Maximum);
            _numAlertMinutes.Value = OverlayForm.ClampInt(_overlay.AlertMinutes, (int)_numAlertMinutes.Minimum, (int)_numAlertMinutes.Maximum);
            _chkAlertEnabled.Checked = _overlay.AlertEnabled;
        }

        private void WireEvents()
        {
            _btnBrowse.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog { SelectedPath = _txtFolder.Text })
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        _txtFolder.Text = dlg.SelectedPath;
                        ApplySettings();
                    }
                }
            };

            _chkWatchEnabled.CheckedChanged += (s, e) =>
            {
                UpdateAlertControlsEnabledState();
                ApplySettings();
            };

            _btnScanNow.Click += (s, e) => _overlay.ScanLatestLogFile(true);

            _numX.ValueChanged += (s, e) => ApplySettings();
            _numY.ValueChanged += (s, e) => ApplySettings();
            _numWidth.ValueChanged += (s, e) => ApplySettings();
            _numHeight.ValueChanged += (s, e) => ApplySettings();

            _trkOpacity.Scroll += (s, e) =>
            {
                _lblOpacityVal.Text = string.Format("{0}%", _trkOpacity.Value);
                ApplySettings();
            };

            _chkBorder.CheckedChanged += (s, e) => ApplySettings();

            _btnBorderColor.Click += (s, e) =>
            {
                using (var dlg = new ColorDialog
                {
                    Color = Color.FromArgb(255, _selectedBorderColor),
                    FullOpen = true
                })
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        int alpha = _selectedBorderColor.A == 0 ? 220 : _selectedBorderColor.A;
                        _selectedBorderColor = Color.FromArgb(alpha, dlg.Color);
                        UpdateBorderColorButtonStyle();
                        ApplySettings();
                    }
                }
            };

            _numFontSize.ValueChanged += (s, e) => ApplySettings();
            _numAlertMinutes.ValueChanged += (s, e) => ApplySettings();
            _chkAlertEnabled.CheckedChanged += (s, e) => ApplySettings();

            _overlay.StatusChanged += msg =>
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    Invoke((MethodInvoker)delegate { _lblStatus.Text = msg; });
                }
                else
                {
                    _lblStatus.Text = msg;
                }
            };

            _overlay.AlertStateChanged += enabled =>
            {
                if (IsDisposed) return;
                MethodInvoker syncCheck = delegate
                {
                    _syncingUi = true;
                    _chkAlertEnabled.Checked = enabled;
                    _syncingUi = false;
                };
                if (InvokeRequired) Invoke(syncCheck);
                else syncCheck();
            };

            FormClosed += (s, e) =>
            {
                _overlay.SaveCurrentSettings();
                _overlay.Close();
            };
        }

        private void LoadWindowIcon()
        {
            try
            {
                Icon resIcon = Resources.nu;
                if (resIcon != null)
                {
                    Icon = (Icon)resIcon.Clone();
                }
            }
            catch { }
        }

        private void UpdateAlertControlsEnabledState()
        {
            bool canUseAlert = _chkWatchEnabled.Checked;
            _numAlertMinutes.Enabled = canUseAlert;
            _chkAlertEnabled.Enabled = canUseAlert;
        }

        private void UpdateBorderColorButtonStyle()
        {
            Color opaque = Color.FromArgb(255, _selectedBorderColor);
            _btnBorderColor.BackColor = opaque;
            double luminance = 0.299 * opaque.R + 0.587 * opaque.G + 0.114 * opaque.B;
            _btnBorderColor.ForeColor = luminance < 140 ? Color.White : Color.Black;
            _btnBorderColor.FlatAppearance.BorderColor = Color.FromArgb(120, 120, 120);
        }

        private void ApplySettings()
        {
            if (_syncingUi) return;

            bool effectiveAlertEnabled = _chkWatchEnabled.Checked && _chkAlertEnabled.Checked;

            _overlay.UpdateOverlaySettings(
                _txtFolder.Text.Trim(),
                _txtTargetText.Text,
                _chkWatchEnabled.Checked,
                (int)_numX.Value,
                (int)_numY.Value,
                (int)_numWidth.Value,
                (int)_numHeight.Value,
                _trkOpacity.Value / 100.0,
                (float)_numFontSize.Value,
                _chkBorder.Checked,
                _selectedBorderColor,
                effectiveAlertEnabled,
                (int)_numAlertMinutes.Value
            );
        }
    }
}