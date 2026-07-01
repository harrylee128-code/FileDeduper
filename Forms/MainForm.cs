using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using FileDeduper.Core;
using FileDeduper.Models;
using FileDeduper.Utils;
using System.Windows.Forms.VisualStyles;

namespace FileDeduper.Forms
{
    /// <summary>
    /// 主界面：文件夹选择 → 扫描 → 查重结果展示 → 智能标记 → 一键删除。
    /// </summary>
    public class MainForm : Form
    {
        // 控件
        private ListBox _folderList;
        private Button _addFolderBtn;
        private Button _removeFolderBtn;
        private CheckBox _includeSubDirsChk;
        private Button _scanBtn;
        private Button _cancelBtn;
        private ComboBox _strategyCombo;
        private Button _autoMarkBtn;
        private Button _verifyHashBtn;
        private TreeView _resultTree;
        private Button _selectAllBtn;
        private Button _invertBtn;
        private Button _deleteBtn;
        private ProgressBar _progressBar;
        private Label _statusLabel;
        private Label _summaryLabel;
        private Button _settingsBtn;
        private Button _aboutBtn;

        // 状态
        private AppSettings _settings;
        private List<DuplicateGroup> _groups;
        private CancellationTokenSource _cts;
        private bool _isBusy;
        private Font _boldFont;  // 重用字体对象，避免内存泄漏

        public MainForm()
        {
            _settings = ConfigStore.Load();
            _groups = new List<DuplicateGroup>();
            _boldFont = null;  // 将在 InitializeComponent 中创建
            InitializeComponent();
            ApplySettingsToUi();
            UpdateSummary();
        }

        private void InitializeComponent()
        {
            this.Text = "FileDeduper  ·  文件查重清理助手";
            this.Width = 1120;
            this.Height = 760;
            this.MinimumSize = new Size(960, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Microsoft YaHei UI", 9F);
            this.BackColor = Color.FromArgb(244, 247, 251);

            // 创建重用的粗体字体对象，避免内存泄漏
            _boldFont = new Font(this.Font, FontStyle.Bold);

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(16);
            root.BackColor = this.BackColor;
            root.ColumnCount = 1;
            root.RowCount = 5;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            this.Controls.Add(root);

            // ---- 顶部：标题与全局操作 ----
            var headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.BackColor = this.BackColor;

            var titleLabel = new Label();
            titleLabel.Text = "FileDeduper";
            titleLabel.Font = new Font(this.Font.FontFamily, 18F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(25, 35, 55);
            titleLabel.Location = new Point(0, 4);
            titleLabel.AutoSize = true;

            var subtitleLabel = new Label();
            subtitleLabel.Text = "重复文件查找、验证与安全清理";
            subtitleLabel.Font = new Font(this.Font.FontFamily, 9.5F);
            subtitleLabel.ForeColor = Color.FromArgb(95, 107, 124);
            subtitleLabel.Location = new Point(2, 38);
            subtitleLabel.AutoSize = true;

            _aboutBtn = new Button();
            _aboutBtn.Text = "关于";
            _aboutBtn.Size = new Size(74, 30);
            _aboutBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _aboutBtn.Location = new Point(headerPanel.Width - 74, 14);
            _aboutBtn.Click += AboutBtn_Click;
            StyleButton(_aboutBtn, ButtonKind.Secondary);

            _settingsBtn = new Button();
            _settingsBtn.Text = "设置";
            _settingsBtn.Size = new Size(74, 30);
            _settingsBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _settingsBtn.Location = new Point(headerPanel.Width - 156, 14);
            _settingsBtn.Click += SettingsBtn_Click;
            StyleButton(_settingsBtn, ButtonKind.Secondary);

            headerPanel.Resize += (sender, e) =>
            {
                _aboutBtn.Location = new Point(headerPanel.ClientSize.Width - _aboutBtn.Width, 14);
                _settingsBtn.Location = new Point(headerPanel.ClientSize.Width - _aboutBtn.Width - _settingsBtn.Width - 8, 14);
            };

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subtitleLabel);
            headerPanel.Controls.Add(_settingsBtn);
            headerPanel.Controls.Add(_aboutBtn);
            root.Controls.Add(headerPanel, 0, 0);

            // ---- 文件夹选择区 ----
            var folderPanel = CreateSectionPanel();
            folderPanel.Padding = new Padding(14, 12, 14, 12);

            var folderLabel = new Label();
            folderLabel.Text = "待扫描文件夹";
            folderLabel.Font = _boldFont;
            folderLabel.ForeColor = Color.FromArgb(30, 41, 59);
            folderLabel.Location = new Point(14, 10);
            folderLabel.AutoSize = true;

            _folderList = new ListBox();
            _folderList.Location = new Point(14, 34);
            _folderList.Size = new Size(710, 76);
            _folderList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _folderList.SelectionMode = SelectionMode.MultiExtended;
            _folderList.HorizontalScrollbar = true;
            _folderList.BorderStyle = BorderStyle.FixedSingle;

            _addFolderBtn = new Button();
            _addFolderBtn.Text = "添加文件夹";
            _addFolderBtn.Size = new Size(112, 32);
            _addFolderBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _addFolderBtn.Click += AddFolderBtn_Click;
            StyleButton(_addFolderBtn, ButtonKind.Secondary);

            _removeFolderBtn = new Button();
            _removeFolderBtn.Text = "移除";
            _removeFolderBtn.Size = new Size(112, 32);
            _removeFolderBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _removeFolderBtn.Click += RemoveFolderBtn_Click;
            StyleButton(_removeFolderBtn, ButtonKind.Secondary);

            _includeSubDirsChk = new CheckBox();
            _includeSubDirsChk.Text = "包含子目录";
            _includeSubDirsChk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _includeSubDirsChk.AutoSize = true;
            _includeSubDirsChk.ForeColor = Color.FromArgb(51, 65, 85);

            _scanBtn = new Button();
            _scanBtn.Text = "开始扫描";
            _scanBtn.Size = new Size(112, 36);
            _scanBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _scanBtn.Click += ScanBtn_Click;
            StyleButton(_scanBtn, ButtonKind.Primary);

            _cancelBtn = new Button();
            _cancelBtn.Text = "取消";
            _cancelBtn.Size = new Size(86, 36);
            _cancelBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _cancelBtn.Enabled = false;
            _cancelBtn.Click += CancelBtn_Click;
            StyleButton(_cancelBtn, ButtonKind.Secondary);

            folderPanel.Resize += (sender, e) =>
            {
                int right = folderPanel.ClientSize.Width - 14;
                _addFolderBtn.Location = new Point(right - 112, 34);
                _removeFolderBtn.Location = new Point(right - 112, 72);
                _includeSubDirsChk.Location = new Point(right - 330, 39);
                _scanBtn.Location = new Point(right - 330, 72);
                _cancelBtn.Location = new Point(right - 210, 72);
                _folderList.Width = Math.Max(320, folderPanel.ClientSize.Width - 470);
                _folderList.Height = Math.Max(58, folderPanel.ClientSize.Height - 46);
            };

            folderPanel.Controls.Add(folderLabel);
            folderPanel.Controls.Add(_folderList);
            folderPanel.Controls.Add(_addFolderBtn);
            folderPanel.Controls.Add(_removeFolderBtn);
            folderPanel.Controls.Add(_includeSubDirsChk);
            folderPanel.Controls.Add(_scanBtn);
            folderPanel.Controls.Add(_cancelBtn);
            root.Controls.Add(folderPanel, 0, 1);

            // ---- 结果操作条 ----
            var toolbarPanel = CreateSectionPanel();
            toolbarPanel.Padding = new Padding(14, 10, 14, 10);

            _summaryLabel = new Label();
            _summaryLabel.Location = new Point(14, 16);
            _summaryLabel.Size = new Size(360, 24);
            _summaryLabel.ForeColor = Color.FromArgb(15, 118, 110);
            _summaryLabel.Font = _boldFont;

            var strategyLabel = new Label();
            strategyLabel.Text = "保留策略：";
            strategyLabel.Location = new Point(386, 17);
            strategyLabel.AutoSize = true;
            strategyLabel.ForeColor = Color.FromArgb(51, 65, 85);

            _strategyCombo = new ComboBox();
            _strategyCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _strategyCombo.Location = new Point(458, 13);
            _strategyCombo.Size = new Size(140, 24);
            _strategyCombo.Items.AddRange(new object[] { "保留最旧", "保留最新", "保留路径最短" });
            _strategyCombo.SelectedIndex = 0;
            _strategyCombo.SelectedIndexChanged += StrategyCombo_SelectedIndexChanged;

            _autoMarkBtn = new Button();
            _autoMarkBtn.Text = "安全标记";
            _autoMarkBtn.Location = new Point(610, 10);
            _autoMarkBtn.Size = new Size(92, 30);
            _autoMarkBtn.Click += AutoMarkBtn_Click;
            StyleButton(_autoMarkBtn, ButtonKind.Secondary);

            _verifyHashBtn = new Button();
            _verifyHashBtn.Text = "MD5 验证";
            _verifyHashBtn.Location = new Point(710, 10);
            _verifyHashBtn.Size = new Size(96, 30);
            _verifyHashBtn.Click += VerifyHashBtn_Click;
            StyleButton(_verifyHashBtn, ButtonKind.Secondary);

            _selectAllBtn = new Button();
            _selectAllBtn.Text = "全选待删";
            _selectAllBtn.Location = new Point(814, 10);
            _selectAllBtn.Size = new Size(90, 30);
            _selectAllBtn.Click += SelectAllBtn_Click;
            StyleButton(_selectAllBtn, ButtonKind.Secondary);

            _invertBtn = new Button();
            _invertBtn.Text = "反选";
            _invertBtn.Location = new Point(912, 10);
            _invertBtn.Size = new Size(72, 30);
            _invertBtn.Click += InvertBtn_Click;
            StyleButton(_invertBtn, ButtonKind.Secondary);

            toolbarPanel.Controls.Add(_summaryLabel);
            toolbarPanel.Controls.Add(strategyLabel);
            toolbarPanel.Controls.Add(_strategyCombo);
            toolbarPanel.Controls.Add(_autoMarkBtn);
            toolbarPanel.Controls.Add(_verifyHashBtn);
            toolbarPanel.Controls.Add(_selectAllBtn);
            toolbarPanel.Controls.Add(_invertBtn);
            root.Controls.Add(toolbarPanel, 0, 2);

            // ---- 结果树 ----
            var resultPanel = CreateSectionPanel();
            resultPanel.Padding = new Padding(1);

            _resultTree = new TreeView();
            _resultTree.Dock = DockStyle.Fill;
            _resultTree.BorderStyle = BorderStyle.None;
            _resultTree.BackColor = Color.White;
            _resultTree.ItemHeight = 24;
            _resultTree.CheckBoxes = false;  // 禁用原生复选框，使用自定义绘制
            _resultTree.ShowNodeToolTips = true;
            _resultTree.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            _resultTree.DrawNode += ResultTree_DrawNode;
            _resultTree.NodeMouseClick += ResultTree_NodeMouseClick;
            _resultTree.NodeMouseDoubleClick += ResultTree_NodeMouseDoubleClick;
            resultPanel.Controls.Add(_resultTree);
            root.Controls.Add(resultPanel, 0, 3);

            // ---- 底部：进度 + 状态 + 删除按钮 ----
            var footerPanel = new Panel();
            footerPanel.Dock = DockStyle.Fill;
            footerPanel.BackColor = this.BackColor;

            _progressBar = new ProgressBar();
            _progressBar.Location = new Point(0, 8);
            _progressBar.Size = new Size(760, 14);
            _progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _progressBar.Minimum = 0;
            _progressBar.Maximum = 100;

            _statusLabel = new Label();
            _statusLabel.Location = new Point(0, 30);
            _statusLabel.Size = new Size(760, 22);
            _statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _statusLabel.ForeColor = Color.FromArgb(71, 85, 105);
            _statusLabel.Text = "就绪。";

            _deleteBtn = new Button();
            _deleteBtn.Text = "删除选中 (0)";
            _deleteBtn.Size = new Size(250, 40);
            _deleteBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _deleteBtn.Enabled = false;
            _deleteBtn.Click += DeleteBtn_Click;
            StyleButton(_deleteBtn, ButtonKind.Danger);
            UpdateDeleteButtonVisual(0);

            footerPanel.Resize += (sender, e) =>
            {
                _deleteBtn.Location = new Point(footerPanel.ClientSize.Width - _deleteBtn.Width, 9);
                _progressBar.Width = Math.Max(240, footerPanel.ClientSize.Width - _deleteBtn.Width - 22);
                _statusLabel.Width = _progressBar.Width;
            };

            footerPanel.Controls.Add(_progressBar);
            footerPanel.Controls.Add(_statusLabel);
            footerPanel.Controls.Add(_deleteBtn);
            root.Controls.Add(footerPanel, 0, 4);

            this.Load += MainForm_Load;
        }

        private enum ButtonKind
        {
            Primary,
            Secondary,
            Danger
        }

        private static Panel CreateSectionPanel()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0, 0, 0, 10);
            panel.BackColor = Color.White;
            panel.BorderStyle = BorderStyle.FixedSingle;
            return panel;
        }

        private static void StyleButton(Button button, ButtonKind kind)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.Font = new Font("Microsoft YaHei UI", 9F);
            button.UseVisualStyleBackColor = false;

            if (kind == ButtonKind.Primary)
            {
                button.BackColor = Color.FromArgb(37, 99, 235);
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderColor = Color.FromArgb(37, 99, 235);
            }
            else if (kind == ButtonKind.Danger)
            {
                button.BackColor = Color.FromArgb(185, 28, 28);
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderColor = Color.FromArgb(185, 28, 28);
            }
            else
            {
                button.BackColor = Color.FromArgb(248, 250, 252);
                button.ForeColor = Color.FromArgb(30, 41, 59);
                button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 恢复上次的文件夹列表
            if (_settings.LastFolders != null)
            {
                foreach (var f in _settings.LastFolders)
                {
                    if (Directory.Exists(f)) _folderList.Items.Add(f);
                }
            }
        }

        private void ApplySettingsToUi()
        {
            _includeSubDirsChk.Checked = _settings.IncludeSubdirectories;
            _strategyCombo.SelectedIndex = (int)_settings.KeepStrategy;
        }

        // ============ 文件夹选择 ============
        private void AddFolderBtn_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择要扫描的文件夹";
                dlg.ShowNewFolderButton = false;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string path = dlg.SelectedPath;
                    if (!_folderList.Items.Contains(path))
                    {
                        _folderList.Items.Add(path);
                        SaveFolderList();
                    }
                }
            }
        }

        private void RemoveFolderBtn_Click(object sender, EventArgs e)
        {
            if (_folderList.SelectedItems.Count == 0) return;
            var toRemove = new List<object>();
            foreach (var item in _folderList.SelectedItems) toRemove.Add(item);
            foreach (var item in toRemove) _folderList.Items.Remove(item);
            SaveFolderList();
        }

        private void SaveFolderList()
        {
            _settings.LastFolders.Clear();
            foreach (var item in _folderList.Items) _settings.LastFolders.Add(item.ToString());
            ConfigStore.Save(_settings);
        }

        // ============ 扫描 + 查重 ============
        private void ScanBtn_Click(object sender, EventArgs e)
        {
            if (_folderList.Items.Count == 0)
            {
                MessageBox.Show(this, "请先添加至少一个文件夹。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_isBusy) return;

            var folders = new List<string>();
            foreach (var item in _folderList.Items) folders.Add(item.ToString());

            _settings.IncludeSubdirectories = _includeSubDirsChk.Checked;
            ConfigStore.Save(_settings);

            _groups.Clear();
            _resultTree.Nodes.Clear();
            SetBusy(true, "扫描中…");
            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.Value = 0;
            _statusLabel.Text = "正在扫描文件…";

            _cts = new CancellationTokenSource();
            var scanner = new FileScanner(folders, _includeSubDirsChk.Checked, _settings.MinFileSize);
            var progress = new Progress<ScanProgress>(p =>
            {
                _statusLabel.Text = string.Format("扫描中… 已扫描 {0} 个文件，跳过 {1} 个，当前：{2}",
                    p.ScannedCount, p.SkippedCount, TruncatePath(p.CurrentDirectory));
            });

            scanner.ScanAsync(progress, _cts.Token).ContinueWith(t =>
            {
                if (this.IsDisposed) return;
                if (t.IsCanceled)
                {
                    this.Invoke((Action)(() =>
                    {
                        SetBusy(false, "已取消扫描。");
                        _progressBar.Style = ProgressBarStyle.Blocks;
                    }));
                    return;
                }
                if (t.IsFaulted)
                {
                    this.Invoke((Action)(() =>
                    {
                        SetBusy(false, "扫描出错：" + (t.Exception != null ? t.Exception.InnerException.Message : "未知错误"));
                        _progressBar.Style = ProgressBarStyle.Blocks;
                    }));
                    return;
                }

                var files = t.Result;
                // 阶段1 快速预筛
                var detector = new DuplicateDetector();
                var groups = detector.FastDetect(files);
                // 自动应用当前策略
                SmartMarker.ApplyStrategy(groups, (KeepStrategy)_strategyCombo.SelectedIndex);

                this.Invoke((Action)(() =>
                {
                    _groups = groups;
                    PopulateTree();
                    _progressBar.Style = ProgressBarStyle.Blocks;
                    _progressBar.Value = 100;
                    SetBusy(false, string.Format("扫描完成：共 {0} 个文件，发现 {1} 组重复。",
                        files.Count, groups.Count));
                    UpdateDeleteButton();
                }));
            });
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            if (_cts != null) _cts.Cancel();
            _statusLabel.Text = "正在取消…";
        }

        // ============ 结果树展示 ============
        private void PopulateTree()
        {
            // 先清空节点，释放内存
            _resultTree.Nodes.Clear();

            // 如果结果太多，只显示前500组避免内存溢出
            int maxGroups = 500;
            int displayedGroups = Math.Min(_groups.Count, maxGroups);

            _resultTree.BeginUpdate();
            try
            {
                for (int i = 0; i < displayedGroups; i++)
                {
                    var g = _groups[i];
                    var gNode = new TreeNode(string.Format("组{0}  ·  {1} 个文件  ·  {2} × {3}  ·  [{4}]",
                        g.Id, g.Files.Count, FileEntry.FormatSize(g.SingleSize), g.TypeText, g.ConfidenceText));
                    gNode.NodeFont = _boldFont;  // 重用字体对象
                    gNode.ForeColor = Color.FromArgb(0, 80, 160);
                    gNode.Tag = g;

                    // 每组最多显示前50个文件节点
                    int maxFiles = 50;
                    int displayedFiles = Math.Min(g.Files.Count, maxFiles);
                    for (int j = 0; j < displayedFiles; j++)
                    {
                        var f = g.Files[j];
                        var fNode = new TreeNode(string.Format("[{0}]  {1}  ·  {2}  ·  {3}",
                            f.IsKeepOriginal ? "保留" : (f.MarkedForDelete ? "删除" : "—"),
                            f.FullPath, f.SizeText, f.ModifiedTimeText));
                        fNode.Tag = f;
                        if (f.IsKeepOriginal) fNode.ForeColor = Color.Green;
                        else if (f.MarkedForDelete) fNode.ForeColor = Color.DarkRed;
                        fNode.ToolTipText = f.FullPath;
                        gNode.Nodes.Add(fNode);
                    }

                    // 如果还有更多文件，添加一个提示节点
                    if (g.Files.Count > maxFiles)
                    {
                        var moreNode = new TreeNode(string.Format("... 还有 {0} 个文件未显示", g.Files.Count - maxFiles));
                        moreNode.ForeColor = Color.Gray;
                        moreNode.NodeFont = this.Font;
                        gNode.Nodes.Add(moreNode);
                    }

                    _resultTree.Nodes.Add(gNode);
                }

                // 如果还有更多组，添加一个提示节点
                if (_groups.Count > maxGroups)
                {
                    var moreNode = new TreeNode(string.Format("... 还有 {0} 组重复文件未显示", _groups.Count - maxGroups));
                    moreNode.ForeColor = Color.Gray;
                    moreNode.NodeFont = this.Font;
                    _resultTree.Nodes.Add(moreNode);
                }
            }
            finally
            {
                _resultTree.EndUpdate();
            }

            UpdateSummary();
        }

        private void ResultTree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            int indent = e.Node.Level * 16; // 每一级缩进16像素
            int iconSize = 9;
            int checkBoxSize = 13;

            // 检查是否点击了展开/折叠图标区域（左侧）
            int iconX = e.Node.Bounds.X + indent;
            int iconY = e.Node.Bounds.Y + (e.Node.Bounds.Height - iconSize) / 2;
            Rectangle iconRect = new Rectangle(iconX, iconY, iconSize, iconSize);

            if (iconRect.Contains(e.Location) && e.Node.Nodes.Count > 0)
            {
                // 切换展开/折叠状态
                if (e.Node.IsExpanded)
                {
                    e.Node.Collapse();
                }
                else
                {
                    e.Node.Expand();
                }
                return;
            }

            // 检查是否点击了复选框区域（右侧）
            int checkBoxX = iconX + iconSize + 4;
            int checkBoxY = e.Node.Bounds.Y + (e.Node.Bounds.Height - checkBoxSize) / 2;
            Rectangle checkBoxRect = new Rectangle(checkBoxX, checkBoxY, checkBoxSize, checkBoxSize);

            if (checkBoxRect.Contains(e.Location))
            {
                // 切换选择状态
                ToggleNodeCheckState(e.Node);
            }
        }

        private void ToggleNodeCheckState(TreeNode node)
        {
            if (node.Tag is FileEntry)
            {
                var f = (FileEntry)node.Tag;
                if (f.IsKeepOriginal) return; // 保留的文件不能切换

                f.MarkedForDelete = !f.MarkedForDelete;
                RefreshFileNode(node);

                if (node.Parent != null && node.Parent.Tag is DuplicateGroup)
                {
                    UpdateGroupNodeCheckState(node.Parent);
                }
            }
            else if (node.Tag is DuplicateGroup)
            {
                // 获取当前状态
                var currentState = GetGroupCheckState(node);
                bool newState = (currentState != CheckState.Checked);

                // 切换所有子文件
                foreach (TreeNode child in node.Nodes)
                {
                    if (child.Tag is FileEntry)
                    {
                        var f = (FileEntry)child.Tag;
                        if (!f.IsKeepOriginal)
                        {
                            f.MarkedForDelete = newState;
                            RefreshFileNode(child);
                        }
                    }
                }
                UpdateGroupNodeCheckState(node);
            }

            UpdateDeleteButton();
            UpdateSummary();
        }

        private void RefreshFileNode(TreeNode node)
        {
            var f = node.Tag as FileEntry;
            if (f == null) return;
            node.Text = string.Format("[{0}]  {1}  ·  {2}  ·  {3}",
                f.IsKeepOriginal ? "保留" : (f.MarkedForDelete ? "删除" : "—"),
                f.FullPath, f.SizeText, f.ModifiedTimeText);
            if (f.IsKeepOriginal) node.ForeColor = Color.Green;
            else if (f.MarkedForDelete) node.ForeColor = Color.DarkRed;
            else node.ForeColor = Color.Black;
        }

        private void ResultTree_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            // 如果节点为空，直接返回
            if (e.Node == null) return;

            // 设置绘制背景
            e.DrawDefault = false;

            // 获取节点背景色
            Color backColor = SystemColors.Window;
            if ((e.State & TreeNodeStates.Selected) != 0)
            {
                backColor = Color.FromArgb(51, 153, 255); // 选中时的蓝色背景
            }

            // 绘制背景
            using (SolidBrush bgBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            // 计算缩进和位置
            int indent = e.Node.Level * 16; // 每一级缩进16像素
            int checkBoxSize = 13;
            int iconSize = 9;

            // 绘制展开/折叠图标（仅组节点），在左侧
            int iconX = e.Bounds.X + indent;
            int iconY = e.Bounds.Y + (e.Bounds.Height - iconSize) / 2;

            if (e.Node.Nodes.Count > 0)
            {
                using (Pen iconPen = new Pen(Color.Gray, 1))
                {
                    // 绘制方框
                    Rectangle iconRect = new Rectangle(iconX, iconY, iconSize, iconSize);
                    e.Graphics.DrawRectangle(iconPen, iconRect);

                    // 绘制加号或减号
                    int centerX = iconX + iconSize / 2;
                    int centerY = iconY + iconSize / 2;
                    e.Graphics.DrawLine(iconPen, centerX, iconY + 2, centerX, iconY + iconSize - 2);

                    if (!e.Node.IsExpanded)
                    {
                        e.Graphics.DrawLine(iconPen, iconX + 2, centerY, iconX + iconSize - 2, centerY);
                    }
                }
            }

            // 绘制复选框，在展开图标右侧
            int checkBoxX = iconX + iconSize + 4;
            int checkBoxY = e.Bounds.Y + (e.Bounds.Height - checkBoxSize) / 2;
            DrawCheckBox(e.Graphics, checkBoxX, checkBoxY, checkBoxSize, GetNodeCheckState(e.Node));

            // 绘制节点文本
            int textX = checkBoxX + checkBoxSize + 6;
            if (e.Node.Nodes.Count > 0) textX += 14; // 为展开图标留空间

            Rectangle textBounds = new Rectangle(
                textX,
                e.Bounds.Y,
                e.Bounds.Width - textX + e.Bounds.X,
                e.Bounds.Height);

            Color textColor = e.Node.ForeColor;
            if ((e.State & TreeNodeStates.Selected) != 0)
            {
                textColor = Color.White;
            }

            TextRenderer.DrawText(e.Graphics, e.Node.Text, e.Node.NodeFont ?? this.Font,
                textBounds, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private CheckState GetNodeCheckState(TreeNode node)
        {
            if (node.Tag is FileEntry)
            {
                var f = (FileEntry)node.Tag;
                return f.MarkedForDelete ? CheckState.Checked : CheckState.Unchecked;
            }
            else if (node.Tag is DuplicateGroup)
            {
                return GetGroupCheckState(node);
            }
            return CheckState.Unchecked;
        }

        private CheckState GetGroupCheckState(TreeNode groupNode)
        {
            int checkedCount = 0;
            int totalCount = 0;

            foreach (TreeNode child in groupNode.Nodes)
            {
                if (child.Tag is FileEntry)
                {
                    totalCount++;
                    var f = (FileEntry)child.Tag;
                    if (f.MarkedForDelete) checkedCount++;
                }
            }

            if (checkedCount == 0) return CheckState.Unchecked;
            if (checkedCount == totalCount) return CheckState.Checked;
            return CheckState.Indeterminate;
        }

        private void UpdateGroupNodeCheckState(TreeNode groupNode)
        {
            var checkState = GetGroupCheckState(groupNode);
            groupNode.Checked = (checkState == CheckState.Checked);

            // 强制重绘以显示部分选择状态
            _resultTree.Invalidate(groupNode.Bounds);
        }

        private void DrawCheckBox(Graphics g, int x, int y, int size, CheckState state)
        {
            // 绘制复选框背景
            using (SolidBrush boxBrush = new SolidBrush(Color.White))
            using (Pen borderPen = new Pen(Color.FromArgb(148, 163, 184)))
            {
                g.FillRectangle(boxBrush, x, y, size, size);
                g.DrawRectangle(borderPen, x, y, size, size);
            }

            if (state == CheckState.Checked)
            {
                // 绘制对勾（√）
                using (Pen checkPen = new Pen(Color.Green, 2))
                {
                    Point[] checkPoints = new Point[]
                    {
                        new Point(x + 2, y + size / 2),
                        new Point(x + size / 2, y + size - 2),
                        new Point(x + size - 2, y + 2)
                    };
                    g.DrawLines(checkPen, checkPoints);
                }
            }
            else if (state == CheckState.Indeterminate)
            {
                // 绘制小绿点（部分选中状态）
                using (SolidBrush dotBrush = new SolidBrush(Color.Green))
                {
                    int dotSize = size / 2;
                    int dotX = x + (size - dotSize) / 2;
                    int dotY = y + (size - dotSize) / 2;
                    g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
                }
            }
        }

        private void ResultTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // 双击文件节点：在资源管理器中定位该文件
            var f = e.Node.Tag as FileEntry;
            if (f == null) return;
            try
            {
                if (File.Exists(f.FullPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + f.FullPath + "\"");
                }
            }
            catch { }
        }

        // ============ 策略切换 / 自动标记 ============
        private void StrategyCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 切换策略后立即对已有结果重算
            if (_groups.Count == 0) return;
            SmartMarker.ApplyStrategy(_groups, (KeepStrategy)_strategyCombo.SelectedIndex);
            PopulateTree();
            UpdateDeleteButton();
        }

        private void AutoMarkBtn_Click(object sender, EventArgs e)
        {
            if (_groups.Count == 0)
            {
                MessageBox.Show(this, "请先扫描出结果。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SmartMarker.ApplyStrategy(_groups, (KeepStrategy)_strategyCombo.SelectedIndex);
            PopulateTree();
            UpdateDeleteButton();
            _statusLabel.Text = "已按「" + _strategyCombo.Text + "」安全标记；疑似组需 MD5 验证或手动选择。";
        }

        private void SelectAllBtn_Click(object sender, EventArgs e)
        {
            foreach (TreeNode gNode in _resultTree.Nodes)
            {
                var group = gNode.Tag as DuplicateGroup;
                if (group != null && group.Confidence == DuplicateConfidence.Suspected) continue;

                foreach (TreeNode fNode in gNode.Nodes)
                {
                    var f = fNode.Tag as FileEntry;
                    if (f != null && !f.IsKeepOriginal)
                    {
                        f.MarkedForDelete = true;
                        RefreshFileNode(fNode);
                    }
                }
                UpdateGroupNodeCheckState(gNode);
            }
            _resultTree.Invalidate();
            UpdateDeleteButton();
            UpdateSummary();
        }

        private void InvertBtn_Click(object sender, EventArgs e)
        {
            foreach (TreeNode gNode in _resultTree.Nodes)
            {
                foreach (TreeNode fNode in gNode.Nodes)
                {
                    var f = fNode.Tag as FileEntry;
                    if (f != null && !f.IsKeepOriginal)
                    {
                        f.MarkedForDelete = !f.MarkedForDelete;
                        RefreshFileNode(fNode);
                    }
                }
                UpdateGroupNodeCheckState(gNode);
            }
            _resultTree.Invalidate();
            UpdateDeleteButton();
            UpdateSummary();
        }

        // ============ 精确验证 (MD5) ============
        private void VerifyHashBtn_Click(object sender, EventArgs e)
        {
            if (_groups.Count == 0)
            {
                MessageBox.Show(this, "请先扫描出结果。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_isBusy) return;

            // 收集需要验证的组：疑似组 + （若设置开启）Likely 组
            var toVerify = new List<DuplicateGroup>();
            foreach (var g in _groups)
            {
                if (g.Confidence == DuplicateConfidence.Suspected ||
                    (g.Confidence == DuplicateConfidence.Likely && _settings.HashVerifyLikelyGroups) ||
                    g.Confidence == DuplicateConfidence.Verified)
                {
                    toVerify.Add(g);
                }
            }
            if (toVerify.Count == 0)
            {
                MessageBox.Show(this, "没有需要验证的组。\n（可在设置中开启「对完全重复组也做哈希复核」）",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetBusy(true, "正在计算 MD5… " + HashEngine.Describe(_settings.HardwareAccelerationMode));
            _progressBar.Value = 0;
            _progressBar.Style = ProgressBarStyle.Blocks;
            _cts = new CancellationTokenSource();

            VerifyGroupsSequentially(toVerify, 0, new List<DuplicateGroup>());
        }

        private void VerifyGroupsSequentially(List<DuplicateGroup> toVerify, int index, List<DuplicateGroup> verified)
        {
            if (index >= toVerify.Count || (_cts != null && _cts.IsCancellationRequested))
            {
                // 完成：用验证后的组替换原组
                var remaining = new List<DuplicateGroup>();
                var verifiedIds = new HashSet<int>();
                foreach (var v in toVerify) verifiedIds.Add(v.Id);
                foreach (var g in _groups)
                {
                    if (!verifiedIds.Contains(g.Id)) remaining.Add(g);
                }
                remaining.AddRange(verified);
                _groups = remaining;
                SmartMarker.ApplyStrategy(_groups, (KeepStrategy)_strategyCombo.SelectedIndex);
                PopulateTree();
                SetBusy(false, string.Format("精确验证完成，确认 {0} 组真实重复。", verified.Count));
                UpdateDeleteButton();
                return;
            }

            var group = toVerify[index];
            var detector = new DuplicateDetector(_settings.HardwareAccelerationMode);
            int totalForGroup = group.Files.Count;
            var progress = new Progress<HashVerifyProgress>(p =>
            {
                int overall = (int)((double)(index * 100 + (p.DoneFiles * 100 / Math.Max(1, totalForGroup))) / Math.Max(1, toVerify.Count));
                _progressBar.Value = Math.Min(100, overall);
                _statusLabel.Text = string.Format("计算 MD5 [{0}/{1}]：{2}",
                    index + 1, toVerify.Count, TruncatePath(p.CurrentFile));
            });

            detector.VerifyByHashAsync(group, progress, _cts.Token).ContinueWith(t =>
            {
                if (this.IsDisposed) return;
                if (t.IsFaulted)
                {
                    this.Invoke((Action)(() => SetBusy(false, "验证出错：" + (t.Exception != null ? t.Exception.InnerException.Message : "未知"))));
                    return;
                }
                var subGroups = t.Result;
                // 给子组重新分配全局唯一 Id
                int maxId = 0;
                foreach (var gg in _groups) if (gg.Id > maxId) maxId = gg.Id;
                foreach (var sg in subGroups) { maxId++; sg.Id = maxId; foreach (var ff in sg.Files) ff.GroupId = maxId; }
                verified.AddRange(subGroups);
                this.Invoke((Action)(() => VerifyGroupsSequentially(toVerify, index + 1, verified)));
            });
        }

        // ============ 删除 ============
        private void DeleteBtn_Click(object sender, EventArgs e)
        {
            var marked = new List<FileEntry>();
            foreach (var g in _groups)
                foreach (var f in g.Files)
                    if (f.MarkedForDelete) marked.Add(f);

            if (marked.Count == 0)
            {
                MessageBox.Show(this, "没有标记删除的文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            long totalBytes = 0;
            foreach (var f in marked) totalBytes += f.Size;

            // 构建删除清单确认
            string modeText = _settings.DeleteMode == DeleteMode.Recycle
                ? "移入回收站（可恢复）" : "永久删除（不可恢复！）";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(string.Format("即将{0} {1} 个文件，共计 {2}。", modeText, marked.Count, FileEntry.FormatSize(totalBytes)));
            sb.AppendLine();
            int show = Math.Min(15, marked.Count);
            for (int i = 0; i < show; i++)
            {
                sb.AppendLine(marked[i].FullPath);
            }
            if (marked.Count > show) sb.AppendLine(string.Format("… 等 {0} 个", marked.Count));
            sb.AppendLine();
            sb.AppendLine("请确认是否继续？");

            var result = MessageBox.Show(this, sb.ToString(), "确认删除",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.OK) return;

            // 执行删除
            SetBusy(true, "正在删除…");
            _statusLabel.Text = "正在删除文件…";
            _progressBar.Style = ProgressBarStyle.Marquee;

            System.Threading.Tasks.Task.Run(() =>
            {
                var delResult = FileDeleter.Delete(marked, _settings.DeleteMode);
                this.Invoke((Action)(() =>
                {
                    _progressBar.Style = ProgressBarStyle.Blocks;
                    SetBusy(false, "");

                    // 从结果中移除已删除项
                    var deletedSet = new HashSet<string>(delResult.DeletedPaths, StringComparer.OrdinalIgnoreCase);
                    foreach (var g in _groups)
                    {
                        for (int i = g.Files.Count - 1; i >= 0; i--)
                        {
                            if (deletedSet.Contains(g.Files[i].FullPath)) g.Files.RemoveAt(i);
                        }
                    }
                    // 移除空组
                    _groups.RemoveAll(g => g.Files.Count < 2);

                    PopulateTree();
                    UpdateDeleteButton();

                    long freed = 0;
                    foreach (var p in delResult.DeletedPaths)
                    {
                        try { /* 大小已不知，用 marked 对照 */ } catch { }
                    }
                    // 用 marked 列表反查大小统计释放量
                    var sizeMap = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                    foreach (var f in marked) sizeMap[f.FullPath] = f.Size;
                    foreach (var p in delResult.DeletedPaths)
                    {
                        long sz;
                        if (sizeMap.TryGetValue(p, out sz)) freed += sz;
                    }

                    if (delResult.AllSucceeded)
                    {
                        _statusLabel.Text = string.Format("已{0} {1} 个文件，释放 {2}。",
                            _settings.DeleteMode == DeleteMode.Recycle ? "回收" : "删除",
                            delResult.DeletedPaths.Count, FileEntry.FormatSize(freed));
                    }
                    else
                    {
                        _statusLabel.Text = string.Format("完成：成功 {0}，失败 {1}。",
                            delResult.DeletedPaths.Count, delResult.Failed.Count);
                        var failSb = new System.Text.StringBuilder();
                        failSb.AppendLine("以下文件未能删除：");
                        foreach (var f in delResult.Failed)
                        {
                            failSb.AppendLine("• " + f.Path + "  —  " + f.Reason);
                        }
                        MessageBox.Show(this, failSb.ToString(), "部分删除失败",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }));
            });
        }

        // ============ 设置 / 关于 ============
        private void SettingsBtn_Click(object sender, EventArgs e)
        {
            using (var dlg = new SettingsForm(_settings))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _settings = dlg.Settings;
                    ConfigStore.Save(_settings);
                    ApplySettingsToUi();
                    // 策略若变了，重算标记
                    if (_groups.Count > 0)
                    {
                        SmartMarker.ApplyStrategy(_groups, _settings.KeepStrategy);
                        PopulateTree();
                        UpdateDeleteButton();
                    }
                }
            }
        }

        private void AboutBtn_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this,
                "文件查重清理助手  ·  绿色版\n\n" +
                "功能：扫描多个文件夹，找出重复文件并智能标记，一键安全删除。\n\n" +
                "• 两阶段查重：快速预筛 + 可选 MD5 精确验证\n" +
                "• 三种保留策略：保留最旧 / 最新 / 路径最短\n" +
                "• 疑似组默认不标记删除，需验证或手动选择\n" +
                "• 删除方式：移入回收站（默认）或永久删除\n" +
                "• 绿色免安装：配置随 exe 同目录走，不写注册表\n\n" +
                "运行环境：.NET Framework 4.8（Win10/11 自带）",
                "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ============ 辅助 ============
        private void SetBusy(bool busy, string status)
        {
            _isBusy = busy;
            _scanBtn.Enabled = !busy;
            _addFolderBtn.Enabled = !busy;
            _removeFolderBtn.Enabled = !busy;
            _verifyHashBtn.Enabled = !busy;
            _deleteBtn.Enabled = !busy && HasMarkedFiles();
            _cancelBtn.Enabled = busy;
            UpdateDeleteButtonVisual(HasMarkedFiles() ? 1 : 0);
            if (!string.IsNullOrEmpty(status)) _statusLabel.Text = status;
        }

        private bool HasMarkedFiles()
        {
            foreach (var g in _groups)
                foreach (var f in g.Files)
                    if (f.MarkedForDelete) return true;
            return false;
        }

        private void UpdateDeleteButton()
        {
            int count = 0;
            long bytes = 0;
            foreach (var g in _groups)
                foreach (var f in g.Files)
                    if (f.MarkedForDelete) { count++; bytes += f.Size; }
            _deleteBtn.Text = string.Format("删除选中 ({0} / {1})", count, FileEntry.FormatSize(bytes));
            _deleteBtn.Enabled = !_isBusy && count > 0;
            UpdateDeleteButtonVisual(count);
            UpdateSummary();
        }

        private void UpdateDeleteButtonVisual(int markedCount)
        {
            if (_deleteBtn == null) return;

            if (!_isBusy && markedCount > 0)
            {
                _deleteBtn.BackColor = Color.FromArgb(185, 28, 28);
                _deleteBtn.ForeColor = Color.White;
                _deleteBtn.FlatAppearance.BorderColor = Color.FromArgb(185, 28, 28);
            }
            else
            {
                _deleteBtn.BackColor = Color.FromArgb(226, 232, 240);
                _deleteBtn.ForeColor = Color.FromArgb(100, 116, 139);
                _deleteBtn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            }
        }

        private void UpdateSummary()
        {
            if (_summaryLabel == null) return;
            int groupCount = _groups.Count;
            long reclaimable = 0;
            int delCount = 0;
            foreach (var g in _groups)
            {
                foreach (var f in g.Files)
                {
                    if (f.MarkedForDelete) { reclaimable += g.SingleSize; delCount++; }
                }
            }
            _summaryLabel.Text = string.Format("共 {0} 组重复，标记删除 {1} 个，可释放 {2}",
                groupCount, delCount, FileEntry.FormatSize(reclaimable));
        }

        private static string TruncatePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (path.Length <= 60) return path;
            return path.Substring(0, 30) + "…" + path.Substring(path.Length - 29);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_boldFont != null)
                {
                    _boldFont.Dispose();
                    _boldFont = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
