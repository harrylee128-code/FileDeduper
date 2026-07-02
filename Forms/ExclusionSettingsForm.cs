using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FileDeduper.Forms
{
    public class ExclusionSettingsForm : Form
    {
        public List<string> ExcludedDirectoryKeywords { get; private set; }
        public List<string> ExcludedFileNameKeywords { get; private set; }

        private TextBox _directoryText;
        private TextBox _fileNameText;
        private Button _okBtn;
        private Button _cancelBtn;

        public ExclusionSettingsForm(List<string> directoryKeywords, List<string> fileNameKeywords)
        {
            ExcludedDirectoryKeywords = CloneList(directoryKeywords);
            ExcludedFileNameKeywords = CloneList(fileNameKeywords);
            InitializeComponent();
            LoadKeywords();
        }

        private void InitializeComponent()
        {
            this.Text = "排除关键词";
            this.Width = 560;
            this.Height = 460;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Microsoft YaHei UI", 9F);

            var dirLabel = new Label();
            dirLabel.Text = "排除文件夹关键词（每行一个；命中目录路径或目录名时跳过整棵目录）";
            dirLabel.Location = new Point(15, 15);
            dirLabel.Size = new Size(510, 22);

            _directoryText = new TextBox();
            _directoryText.Location = new Point(15, 40);
            _directoryText.Size = new Size(510, 120);
            _directoryText.Multiline = true;
            _directoryText.ScrollBars = ScrollBars.Vertical;
            _directoryText.AcceptsReturn = true;

            var fileLabel = new Label();
            fileLabel.Text = "排除文件名关键词（每行一个；只匹配文件名，不匹配父目录）";
            fileLabel.Location = new Point(15, 175);
            fileLabel.Size = new Size(510, 22);

            _fileNameText = new TextBox();
            _fileNameText.Location = new Point(15, 200);
            _fileNameText.Size = new Size(510, 120);
            _fileNameText.Multiline = true;
            _fileNameText.ScrollBars = ScrollBars.Vertical;
            _fileNameText.AcceptsReturn = true;

            var hint = new Label();
            hint.Text = "匹配规则：大小写不敏感，按包含关系匹配；空行会被忽略。排除规则在下一次扫描生效。";
            hint.Location = new Point(15, 330);
            hint.Size = new Size(510, 38);
            hint.ForeColor = Color.FromArgb(71, 85, 105);

            _okBtn = new Button();
            _okBtn.Text = "确定";
            _okBtn.Location = new Point(350, 380);
            _okBtn.Size = new Size(80, 30);
            _okBtn.Click += OkBtn_Click;

            _cancelBtn = new Button();
            _cancelBtn.Text = "取消";
            _cancelBtn.Location = new Point(445, 380);
            _cancelBtn.Size = new Size(80, 30);
            _cancelBtn.Click += CancelBtn_Click;

            this.Controls.Add(dirLabel);
            this.Controls.Add(_directoryText);
            this.Controls.Add(fileLabel);
            this.Controls.Add(_fileNameText);
            this.Controls.Add(hint);
            this.Controls.Add(_okBtn);
            this.Controls.Add(_cancelBtn);
            this.AcceptButton = _okBtn;
            this.CancelButton = _cancelBtn;
        }

        private void LoadKeywords()
        {
            _directoryText.Text = JoinKeywords(ExcludedDirectoryKeywords);
            _fileNameText.Text = JoinKeywords(ExcludedFileNameKeywords);
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            ExcludedDirectoryKeywords = ParseKeywords(_directoryText.Text);
            ExcludedFileNameKeywords = ParseKeywords(_fileNameText.Text);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private static string JoinKeywords(List<string> values)
        {
            if (values == null || values.Count == 0) return "";
            return string.Join(Environment.NewLine, values.ToArray());
        }

        private static List<string> ParseKeywords(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) return result;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string keyword = line.Trim();
                if (seen.Add(keyword)) result.Add(keyword);
            }
            return result;
        }

        private static List<string> CloneList(List<string> values)
        {
            var result = new List<string>();
            if (values == null) return result;
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value)) result.Add(value.Trim());
            }
            return result;
        }
    }
}
