using System.Drawing;
using System.Windows.Forms;

namespace TWChatLog_Nu
{
    public partial class ControlForm
    {
        private System.ComponentModel.IContainer components = null;

        private GroupBox _grpWatch;
        private Label _lblFolder;
        private TextBox _txtFolder;
        private Button _btnBrowse;
        private Label _lblTarget;
        private TextBox _txtTargetText;
        private CheckBox _chkWatchEnabled;
        private Button _btnScanNow;

        private GroupBox _grpOverlay;
        private Label _lblXy;
        private NumericUpDown _numX;
        private NumericUpDown _numY;
        private Label _lblSize;
        private NumericUpDown _numWidth;
        private NumericUpDown _numHeight;
        private Label _lblOpacity;
        private TrackBar _trkOpacity;
        private Label _lblOpacityVal;
        private CheckBox _chkBorder;
        private Button _btnBorderColor;
        private Label _lblFont;
        private NumericUpDown _numFontSize;

        private GroupBox _grpAlert;
        private Label _lblAlert;
        private NumericUpDown _numAlertMinutes;
        private CheckBox _chkAlertEnabled;

        private GroupBox _grpAction;
        private Label _lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            const int outerMargin = 12;
            const int groupWidth = 468;
            const int innerLeft = 14;
            const int labelWidth = 86;
            const int inputLeft = innerLeft + labelWidth + 6; // 106
            const int innerRight = groupWidth - 14; // 454
            const int fullInputWidth = innerRight - inputLeft; // 348
            const int browseBtnWidth = 76;

            int curY = outerMargin;

            _grpWatch = new GroupBox();
            _lblFolder = new Label();
            _txtFolder = new TextBox();
            _btnBrowse = new Button();
            _lblTarget = new Label();
            _txtTargetText = new TextBox();
            _chkWatchEnabled = new CheckBox();
            _btnScanNow = new Button();

            _grpOverlay = new GroupBox();
            _lblXy = new Label();
            _numX = new NumericUpDown();
            _numY = new NumericUpDown();
            _lblSize = new Label();
            _numWidth = new NumericUpDown();
            _numHeight = new NumericUpDown();
            _lblOpacity = new Label();
            _trkOpacity = new TrackBar();
            _lblOpacityVal = new Label();
            _chkBorder = new CheckBox();
            _btnBorderColor = new Button();
            _lblFont = new Label();
            _numFontSize = new NumericUpDown();

            _grpAlert = new GroupBox();
            _lblAlert = new Label();
            _numAlertMinutes = new NumericUpDown();
            _chkAlertEnabled = new CheckBox();

            _grpAction = new GroupBox();
            _lblStatus = new Label();

            SuspendLayout();

            // 1. 로그 감시 그룹박스
            int gy = 24;
            _grpWatch.Text = "로그 감시";
            _grpWatch.Location = new Point(outerMargin, curY);
            _grpWatch.Size = new Size(groupWidth, 134);

            ConfigureRowLabel(_lblFolder, "로그 폴더:", innerLeft, gy + 3, labelWidth);
            _txtFolder.Location = new Point(inputLeft, gy);
            _txtFolder.Size = new Size(fullInputWidth - browseBtnWidth - 8, 24);

            _btnBrowse.Text = "찾아보기";
            _btnBrowse.Location = new Point(innerRight - browseBtnWidth, gy - 1);
            _btnBrowse.Size = new Size(browseBtnWidth, 26);

            gy += 34;
            ConfigureRowLabel(_lblTarget, "감시 문구:", innerLeft, gy + 3, labelWidth);
            _txtTargetText.Location = new Point(inputLeft, gy);
            _txtTargetText.Size = new Size(fullInputWidth, 24);

            gy += 32;
            _chkWatchEnabled.Text = "로그 감시 활성화";
            _chkWatchEnabled.Location = new Point(inputLeft, gy);
            _chkWatchEnabled.Size = new Size(fullInputWidth - 116, 22);

            _btnScanNow.Text = "로그 즉시 스캔";
            _btnScanNow.Location = new Point(innerRight - 108, gy - 2);
            _btnScanNow.Size = new Size(108, 26);

            _grpWatch.Controls.AddRange(new Control[]
            {
                _lblFolder, _txtFolder, _btnBrowse,
                _lblTarget, _txtTargetText,
                _chkWatchEnabled, _btnScanNow
            });

            curY += _grpWatch.Height + 10;

            // 2. 오버레이 표시 그룹박스
            gy = 24;
            _grpOverlay.Text = "오버레이 표시";
            _grpOverlay.Location = new Point(outerMargin, curY);
            _grpOverlay.Size = new Size(groupWidth, 144);

            ConfigureRowLabel(_lblXy, "X / Y 좌표:", innerLeft, gy + 3, labelWidth);
            ConfigureNumeric(_numX, -5000, 10000, inputLeft, gy, 62);
            ConfigureNumeric(_numY, -5000, 10000, inputLeft + 68, gy, 62);

            _lblSize.Text = "너비 / 높이:";
            _lblSize.Location = new Point(242, gy + 3);
            _lblSize.Size = new Size(76, 20);
            _lblSize.TextAlign = ContentAlignment.MiddleLeft;

            ConfigureNumeric(_numWidth, 160, 2500, 320, gy, 62);
            ConfigureNumeric(_numHeight, 80, 1200, 388, gy, 66);

            gy += 36;
            ConfigureRowLabel(_lblOpacity, "투명도:", innerLeft, gy + 3, labelWidth);

            _trkOpacity.AutoSize = false;
            _trkOpacity.Minimum = 20;
            _trkOpacity.Maximum = 100;
            _trkOpacity.TickFrequency = 10;
            _trkOpacity.Location = new Point(inputLeft - 4, gy);
            _trkOpacity.Size = new Size(176, 28);

            _lblOpacityVal.Location = new Point(284, gy + 3);
            _lblOpacityVal.Size = new Size(42, 20);
            _lblOpacityVal.TextAlign = ContentAlignment.MiddleLeft;

            _chkBorder.Text = "외곽선";
            _chkBorder.Location = new Point(328, gy + 3);
            _chkBorder.Size = new Size(68, 22);

            _btnBorderColor.Text = "색상";
            _btnBorderColor.Location = new Point(398, gy + 1);
            _btnBorderColor.Size = new Size(innerRight - 398, 25);
            _btnBorderColor.FlatStyle = FlatStyle.Flat;

            gy += 38;
            ConfigureRowLabel(_lblFont, "글자 크기:", innerLeft, gy + 3, labelWidth);
            ConfigureNumeric(_numFontSize, 9, 48, inputLeft, gy + 2, 66);

            _grpOverlay.Controls.AddRange(new Control[]
            {
                _lblXy, _numX, _numY, _lblSize, _numWidth, _numHeight,
                _lblOpacity, _trkOpacity, _lblOpacityVal, _chkBorder, _btnBorderColor,
                _lblFont, _numFontSize
            });

            curY += _grpOverlay.Height + 10;

            // 3. 누 출현 대기 알림 그룹박스
            gy = 25;
            _grpAlert.Text = "누 출현 대기 알림";
            _grpAlert.Location = new Point(outerMargin, curY);
            _grpAlert.Size = new Size(groupWidth, 64);

            ConfigureRowLabel(_lblAlert, "대기 시간:", innerLeft, gy + 3, labelWidth);
            ConfigureNumeric(_numAlertMinutes, 1, 180, inputLeft, gy + 2, 66);

            _chkAlertEnabled.Text = "지정 시간 뒤 알림 (분)";
            _chkAlertEnabled.Location = new Point(182, gy + 2);
            _chkAlertEnabled.Size = new Size(innerRight - 182, 24);

            _grpAlert.Controls.AddRange(new Control[]
            {
                _lblAlert, _numAlertMinutes, _chkAlertEnabled
            });

            curY += _grpAlert.Height + 10;

            // 4. 동작 및 상태 그룹박스
            gy = 22;
            _grpAction.Text = "동작 및 상태";
            _grpAction.Location = new Point(outerMargin, curY);
            _grpAction.Size = new Size(groupWidth, 78);

            _lblStatus.Text = "상태: 대기 중...";
            _lblStatus.Location = new Point(innerLeft, gy);
            _lblStatus.Size = new Size(innerRight - innerLeft, 48);
            _lblStatus.ForeColor = Color.FromArgb(75, 75, 75);

            _grpAction.Controls.Add(_lblStatus);

            // Form 설정
            Text = "TalesWeaver [Nu] Mature";
            ClientSize = new Size(492, 476);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(100, 100);
            Font = new Font("맑은 고딕", 9f, FontStyle.Regular);

            Controls.AddRange(new Control[]
            {
                _grpWatch,
                _grpOverlay,
                _grpAlert,
                _grpAction
            });

            ResumeLayout(false);
        }

        private static void ConfigureRowLabel(Label lbl, string text, int x, int y, int width)
        {
            lbl.Text = text;
            lbl.Location = new Point(x, y);
            lbl.Size = new Size(width, 20);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
        }

        private static void ConfigureNumeric(NumericUpDown num, int min, int max, int x, int y, int w)
        {
            num.Minimum = min;
            num.Maximum = max;
            num.Location = new Point(x, y);
            num.Size = new Size(w, 24);
        }
    }
}