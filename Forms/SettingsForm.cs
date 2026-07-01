using System;
using System.Drawing;
using System.Windows.Forms;
using FileDeduper.Models;
using FileDeduper.Utils;

namespace FileDeduper.Forms
{
    /// <summary>
    /// 设置弹窗：删除方式、保留策略、子目录、哈希阈值、是否复核 Likely 组、最小文件大小。
    /// </summary>
    public class SettingsForm : Form
    {
        public AppSettings Settings { get; private set; }

        private RadioButton _recycleRadio;
        private RadioButton _permanentRadio;
        private RadioButton _oldestRadio;
        private RadioButton _newestRadio;
        private RadioButton _shortestRadio;
        private CheckBox _includeSubDirsChk;
        private CheckBox _verifyLikelyChk;
        private RadioButton _accelAutoRadio;
        private RadioButton _accelCpuRadio;
        private RadioButton _accelGpuRadio;
        private NumericUpDown _minSizeUpDown;
        private NumericUpDown _hashParallelismUpDown;
        private Button _okBtn;
        private Button _cancelBtn;

        public SettingsForm(AppSettings settings)
        {
            Settings = settings;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "设置";
            this.Width = 460;
            this.Height = 590;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Microsoft YaHei UI", 9F);

            int y = 15;

            // 删除方式
            var delGroup = new GroupBox();
            delGroup.Text = "默认删除方式";
            delGroup.Location = new Point(15, y);
            delGroup.Size = new Size(410, 75);

            _recycleRadio = new RadioButton();
            _recycleRadio.Text = "移入回收站（可恢复，推荐）";
            _recycleRadio.Location = new Point(15, 25);
            _recycleRadio.AutoSize = true;
            _recycleRadio.Checked = true;

            _permanentRadio = new RadioButton();
            _permanentRadio.Text = "永久删除（不可恢复，需二次确认）";
            _permanentRadio.Location = new Point(15, 48);
            _permanentRadio.AutoSize = true;

            delGroup.Controls.Add(_recycleRadio);
            delGroup.Controls.Add(_permanentRadio);
            this.Controls.Add(delGroup);
            y += 85;

            // 保留策略
            var keepGroup = new GroupBox();
            keepGroup.Text = "默认保留策略（自动标记时保留哪个副本）";
            keepGroup.Location = new Point(15, y);
            keepGroup.Size = new Size(410, 105);

            _oldestRadio = new RadioButton();
            _oldestRadio.Text = "保留修改时间最旧（保留原始）";
            _oldestRadio.Location = new Point(15, 25);
            _oldestRadio.AutoSize = true;
            _oldestRadio.Checked = true;

            _newestRadio = new RadioButton();
            _newestRadio.Text = "保留修改时间最新（保留更新）";
            _newestRadio.Location = new Point(15, 48);
            _newestRadio.AutoSize = true;

            _shortestRadio = new RadioButton();
            _shortestRadio.Text = "保留路径最短（最靠近根目录）";
            _shortestRadio.Location = new Point(15, 71);
            _shortestRadio.AutoSize = true;

            keepGroup.Controls.Add(_oldestRadio);
            keepGroup.Controls.Add(_newestRadio);
            keepGroup.Controls.Add(_shortestRadio);
            this.Controls.Add(keepGroup);
            y += 115;

            // 扫描与哈希
            var scanGroup = new GroupBox();
            scanGroup.Text = "扫描与哈希";
            scanGroup.Location = new Point(15, y);
            scanGroup.Size = new Size(410, 138);

            _includeSubDirsChk = new CheckBox();
            _includeSubDirsChk.Text = "默认包含子目录";
            _includeSubDirsChk.Location = new Point(15, 25);
            _includeSubDirsChk.AutoSize = true;
            _includeSubDirsChk.Checked = true;

            _verifyLikelyChk = new CheckBox();
            _verifyLikelyChk.Text = "对完全重复组也做 MD5 复核（更慢但更准）";
            _verifyLikelyChk.Location = new Point(15, 48);
            _verifyLikelyChk.AutoSize = true;

            var minLabel = new Label();
            minLabel.Text = "忽略小于此大小的文件 (KB)：";
            minLabel.Location = new Point(15, 76);
            minLabel.AutoSize = true;

            _minSizeUpDown = new NumericUpDown();
            _minSizeUpDown.Location = new Point(210, 73);
            _minSizeUpDown.Size = new Size(90, 22);
            _minSizeUpDown.Minimum = 0;
            _minSizeUpDown.Maximum = 1048576;
            _minSizeUpDown.Value = 0;

            var parallelLabel = new Label();
            parallelLabel.Text = "哈希并行度（0=自动）：";
            parallelLabel.Location = new Point(15, 104);
            parallelLabel.AutoSize = true;

            _hashParallelismUpDown = new NumericUpDown();
            _hashParallelismUpDown.Location = new Point(210, 101);
            _hashParallelismUpDown.Size = new Size(90, 22);
            _hashParallelismUpDown.Minimum = 0;
            _hashParallelismUpDown.Maximum = HashParallelism.Maximum;
            _hashParallelismUpDown.Value = 0;

            scanGroup.Controls.Add(_includeSubDirsChk);
            scanGroup.Controls.Add(_verifyLikelyChk);
            scanGroup.Controls.Add(minLabel);
            scanGroup.Controls.Add(_minSizeUpDown);
            scanGroup.Controls.Add(parallelLabel);
            scanGroup.Controls.Add(_hashParallelismUpDown);
            this.Controls.Add(scanGroup);
            y += 148;

            // 硬件加速
            var accelGroup = new GroupBox();
            accelGroup.Text = "哈希计算加速";
            accelGroup.Location = new Point(15, y);
            accelGroup.Size = new Size(410, 105);

            _accelAutoRadio = new RadioButton();
            _accelAutoRadio.Text = "自动（推荐：可用时使用安全 provider，否则 CPU）";
            _accelAutoRadio.Location = new Point(15, 24);
            _accelAutoRadio.AutoSize = true;
            _accelAutoRadio.Checked = true;

            _accelCpuRadio = new RadioButton();
            _accelCpuRadio.Text = "仅 CPU（最稳定，绿色便携）";
            _accelCpuRadio.Location = new Point(15, 48);
            _accelCpuRadio.AutoSize = true;

            _accelGpuRadio = new RadioButton();
            _accelGpuRadio.Text = "GPU 实验模式（无 provider 时自动回退 CPU）";
            _accelGpuRadio.Location = new Point(15, 72);
            _accelGpuRadio.AutoSize = true;

            accelGroup.Controls.Add(_accelAutoRadio);
            accelGroup.Controls.Add(_accelCpuRadio);
            accelGroup.Controls.Add(_accelGpuRadio);
            this.Controls.Add(accelGroup);
            y += 115;

            // 按钮
            _okBtn = new Button();
            _okBtn.Text = "确定";
            _okBtn.Location = new Point(250, y);
            _okBtn.Size = new Size(80, 30);
            _okBtn.Click += OkBtn_Click;

            _cancelBtn = new Button();
            _cancelBtn.Text = "取消";
            _cancelBtn.Location = new Point(345, y);
            _cancelBtn.Size = new Size(80, 30);
            _cancelBtn.Click += CancelBtn_Click;

            this.Controls.Add(_okBtn);
            this.Controls.Add(_cancelBtn);

            this.AcceptButton = _okBtn;
            this.CancelButton = _cancelBtn;
        }

        private void LoadSettings()
        {
            if (Settings == null) Settings = new AppSettings();
            _recycleRadio.Checked = Settings.DeleteMode == DeleteMode.Recycle;
            _permanentRadio.Checked = Settings.DeleteMode == DeleteMode.Permanent;
            _oldestRadio.Checked = Settings.KeepStrategy == KeepStrategy.Oldest;
            _newestRadio.Checked = Settings.KeepStrategy == KeepStrategy.Newest;
            _shortestRadio.Checked = Settings.KeepStrategy == KeepStrategy.ShortestPath;
            _includeSubDirsChk.Checked = Settings.IncludeSubdirectories;
            _verifyLikelyChk.Checked = Settings.HashVerifyLikelyGroups;
            _accelAutoRadio.Checked = Settings.HardwareAccelerationMode == HardwareAccelerationMode.Auto;
            _accelCpuRadio.Checked = Settings.HardwareAccelerationMode == HardwareAccelerationMode.CpuOnly;
            _accelGpuRadio.Checked = Settings.HardwareAccelerationMode == HardwareAccelerationMode.GpuExperimental;
            _minSizeUpDown.Value = Math.Max(0, Math.Min(_minSizeUpDown.Maximum, Settings.MinFileSize / 1024));
            _hashParallelismUpDown.Value = Math.Max(_hashParallelismUpDown.Minimum, Math.Min(_hashParallelismUpDown.Maximum, Settings.HashParallelism));
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            Settings.DeleteMode = _recycleRadio.Checked ? DeleteMode.Recycle : DeleteMode.Permanent;
            Settings.KeepStrategy = _oldestRadio.Checked ? KeepStrategy.Oldest
                                 : _newestRadio.Checked ? KeepStrategy.Newest
                                 : KeepStrategy.ShortestPath;
            Settings.IncludeSubdirectories = _includeSubDirsChk.Checked;
            Settings.HashVerifyLikelyGroups = _verifyLikelyChk.Checked;
            Settings.HardwareAccelerationMode = _accelCpuRadio.Checked ? HardwareAccelerationMode.CpuOnly
                                              : _accelGpuRadio.Checked ? HardwareAccelerationMode.GpuExperimental
                                              : HardwareAccelerationMode.Auto;
            Settings.HashParallelism = HashParallelism.NormalizeForSettings((int)_hashParallelismUpDown.Value);
            Settings.MinFileSize = (long)_minSizeUpDown.Value * 1024;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
