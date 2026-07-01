# FileDeduper 开发文档

## 项目简介

FileDeduper 是一个绿色便携的Windows重复文件查找和清理工具，使用 C# WinForms 开发，无需安装，双击即可使用。

### 核心功能
- 多文件夹扫描，支持子目录递归
- 两阶段重复文件检测（快速预筛 + 可选全量 MD5 精确验证）
- 智能标记策略（保留最旧/最新/路径最短）
- 疑似组默认不标记删除，避免同大小文件误删
- 安全删除（移入回收站或永久删除）
- 绿色便携，配置随程序同目录存储

---

## 快速开始

### 环境要求
- Windows 10/11
- .NET Framework 4.8（系统自带）
- 无需 Visual Studio 或 .NET SDK

### 编译步骤

1. **编译主程序**
   ```cmd
   cd FileDeduper
   build.cmd
   ```
   生成的 exe 位于：`bin\Release\FileDeduper.exe`

2. **编译测试程序**（可选）
   ```cmd
   build-test.cmd
   ```
   生成的测试程序位于：`bin\Test\FileDeduper.Test.exe`

3. **运行程序**
   - 双击 `bin\Release\FileDeduper.exe` 启动
   - 或复制到任何位置直接运行

---

## 项目结构

```
FileDeduper/
├── bin/                          # 编译输出目录
│   ├── Release/                  # 主程序输出
│   │   └── FileDeduper.exe       # 最终可执行文件
│   └── Test/                     # 测试程序输出
│       └── FileDeduper.Test.exe  # 测试可执行文件
├── Core/                         # 核心业务逻辑
│   ├── DuplicateDetector.cs      # 重复文件检测（两阶段算法）
│   ├── FileDeleter.cs            # 文件删除逻辑（回收站/永久删除）
│   ├── FileScanner.cs            # 文件扫描引擎（递归扫描）
│   └── SmartMarker.cs            # 智能标记策略
├── Forms/                        # 界面层
│   ├── MainForm.cs               # 主界面（TreeView + 操作按钮）
│   └── SettingsForm.cs           # 设置对话框
├── Models/                       # 数据模型
│   ├── AppSettings.cs            # 应用配置模型
│   ├── DuplicateGroup.cs         # 重复组模型
│   ├── Enums.cs                  # 枚举定义（策略、模式等）
│   └── FileEntry.cs              # 文件条目模型
├── Properties/                   # 程序集属性
│   └── AssemblyInfo.cs           # 版本信息等
├── Tests/                        # 自动化测试
│   └── SelfTest.cs               # 核心功能测试套件
├── Utils/                        # 工具类库
│   ├── ConfigStore.cs            # JSON 配置存储
│   ├── HashHelper.cs             # MD5 哈希计算（全量分块读取）
│   ├── MiniJson.cs               # 自定义 JSON 解析器
│   └── RecycleBinHelper.cs       # 回收站操作（P/Invoke）
├── Program.cs                    # 程序入口（DPI 感知）
├── build.cmd                     # 主程序编译脚本
├── build-test.cmd                # 测试程序编译脚本
├── Trash.ico                     # 应用图标
├── app.rc                        # 资源文件（可选）
└── README-DEV.md                 # 本开发文档
```

---

## 技术架构

### 1. 核心算法

#### 两阶段重复文件检测
```csharp
// 阶段1：快速预筛（大小 + 名称 + 修改时间）
var groups = detector.FastDetect(files);

// 阶段2：MD5 精确验证（可选）
detector.VerifyByHashAsync(group, progress, token);
```

#### 智能标记策略
```csharp
// 三种保留策略
SmartMarker.ApplyStrategy(groups, KeepStrategy.Oldest);     // 保留最旧
SmartMarker.ApplyStrategy(groups, KeepStrategy.Newest);     // 保留最新
SmartMarker.ApplyStrategy(groups, KeepStrategy.ShortestPath); // 保留路径最短
```

疑似组仅作为候选展示，不会被自动标记删除；通过全量 MD5 验证后产生的 `Verified` 组才会自动标记。

### 2. 界面技术

#### 自定义 TreeView
- **完全自定义绘制**：`DrawMode = TreeViewDrawMode.OwnerDrawAll`
- **三种复选框状态**：空白框 / 小绿点（部分选中） / 对勾（全选）
- **精确点击检测**：区分展开按钮和复选框点击区域

#### DPI 感知
```csharp
// 通过 P/Invoke 设置 DPI 感知
SetProcessDPIAware();
```

### 3. 安全删除

#### 多层删除策略
```csharp
1. 尝试移入回收站（SHFileOperation API）
2. 失败后清除文件属性重试回收站
3. 仍失败则报告失败并保留原文件，绝不在回收站模式下静默永久删除
4. 网络路径、非固定盘或无法确认支持回收站的路径不进入删除 API，直接报告失败并保留文件
```

---

## 关键代码说明

### DuplicateDetector.cs
**核心类：两阶段重复文件检测**

```csharp
public class DuplicateDetector
{
    // 快速预筛：基于大小、名称、修改时间
    public List<DuplicateGroup> FastDetect(List<FileEntry> files)

    // 精确验证：基于 MD5 哈希
    public Task<List<DuplicateGroup>> VerifyByHashAsync(
        DuplicateGroup group, IProgress<HashVerifyProgress> progress,
        CancellationToken token)
}
```

**算法要点：**
- 首先按文件大小分组，快速排除不可能重复的文件
- 然后按文件名和修改时间进一步筛选
- 最后用全量 MD5 确认真实的重复文件
- “已验证”只表示完整文件哈希一致，不使用抽样哈希

### FileDeleter.cs
**核心类：安全删除逻辑**

```csharp
public static class FileDeleter
{
    public static DeleteResult Delete(List<FileEntry> entries, DeleteMode mode)
}
```

**删除流程：**
1. 过滤掉不存在或未标记删除的文件
2. 根据 `DeleteMode` 选择删除方式：
   - `DeleteMode.Recycle`：移入回收站
   - `DeleteMode.Permanent`：永久删除
3. 处理失败情况，提供详细错误信息

### MainForm.cs
**主界面类：TreeView 自定义绘制**

```csharp
private void ResultTree_DrawNode(object sender, DrawTreeNodeEventArgs e)
{
    // 1. 绘制节点背景
    // 2. 绘制展开/折叠图标（左侧）
    // 3. 绘制复选框（右侧，支持三种状态）
    // 4. 绘制节点文本
}
```

**自定义复选框状态：**
- `CheckState.Unchecked`：空白框
- `CheckState.Indeterminate`：小绿点（部分选中）
- `CheckState.Checked`：对勾（全选）

---

## 编译脚本说明

### build.cmd
编译主程序 GUI 版本

```batch
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set OUTDIR=%ROOT%bin\Release

"%CSC%" /target:winexe /platform:anycpu /langversion:5 /optimize+ ^
    /out:"%OUTDIR%\FileDeduper.exe" ^
    /win32icon:"%ROOT%Trash.ico" ^
    /reference:System.dll /reference:System.Core.dll ^
    /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
    [源文件列表]
```

**关键参数：**
- `/target:winexe`：生成 Windows GUI 应用程序（无控制台窗口）
- `/langversion:5`：使用 C# 5.0 语法（兼容旧版 .NET Framework）
- `/win32icon`：嵌入图标文件

### build-test.cmd
编译命令行测试程序

```batch
"%CSC%" /target:exe /platform:anycpu /langversion:5 /optimize+ ^
    /out:"%OUTDIR%\FileDeduper.Test.exe" ^
    /reference:System.dll /reference:System.Core.dll ^
    [源文件列表] Tests\SelfTest.cs
```

**关键参数：**
- `/target:exe`：生成控制台应用程序

---

## 依赖说明

### 系统依赖
- .NET Framework 4.8（Windows 10/11 自带）
- Windows Shell API（用于回收站操作）

### 无外部依赖
- 不依赖 Newtonsoft.Json（使用自定义 MiniJson）
- 不依赖第三方 UI 框架（使用原生 WinForms）
- 不依赖 .NET SDK（使用系统自带 csc.exe）

---

## 扩展开发

### GPU / 硬件加速边界

当前核心查重链路提供 optional hardware acceleration 抽象和 benchmark，但不把 CUDA、OpenCL 或 Intel runtime 作为基础依赖。原因：

- 基础重复文件查找主要受磁盘 I/O、目录枚举和安全删除约束。
- 全量 MD5 使用流式分块读取，CPU 成本通常不是主瓶颈。
- GPU 依赖会增加驱动、运行时和分发复杂度，不符合 v2 绿色便携目标。

如果未来增加图片相似度、视频指纹或 AI 内容识别，可作为可选模块接入 CUDA、DirectML、OpenCL、OpenVINO 或 ONNX Runtime GPU。

哈希 benchmark：

```cmd
build-test.cmd
bin\Test\FileDeduper.Test.exe --benchmark <folder>
```

### 添加新的保留策略
1. 在 `Models/Enums.cs` 中添加新的 `KeepStrategy` 枚举值
2. 在 `Core/SmartMarker.cs` 中实现对应的标记逻辑
3. 在 `Forms/MainForm.cs` 的界面中添加选项

### 修改删除行为
1. 查看 `Core/FileDeleter.cs`
2. 修改 `Delete()` 方法的实现
3. 调整 `Utils/RecycleBinHelper.cs` 中的 P/Invoke 调用

### 自定义界面样式
1. 主要在 `Forms/MainForm.cs` 中修改
2. `ResultTree_DrawNode()` 方法控制 TreeView 绘制
3. `InitializeComponent()` 方法控制控件布局

---

## 测试说明

### 运行自动化测试
```cmd
cd FileDeduper
build-test.cmd
bin\Test\FileDeduper.Test.exe
```

### 测试覆盖
- 文件扫描（递归/非递归）
- 重复检测（快速预筛 + MD5 验证）
- 智能标记（三种策略）
- 文件删除（回收站/永久删除）
- 回收站路径能力预检（UNC/network 路径不会进入删除 API）

---

## 常见问题

### Q: 编译失败，提示找不到 csc.exe
**A:** 确认系统中存在以下路径之一：
```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
```

### Q: 程序运行时出现 OutOfMemoryException
**A:** 当前版本已限制 TreeView 显示节点数量（最多 500 组，每组 50 个文件），避免内存溢出。

### Q: 文件删除失败，提示权限不足
**A:** 程序已实现多层删除策略：
1. 自动清除文件属性（只读/隐藏等）
2. 回收站模式失败时不会 fallback 到永久删除
3. 提供详细的失败原因

### Q: 如何添加自定义图标
**A:** 将 .ico 文件放在项目根目录，修改 `build.cmd`：
```batch
/win32icon:"%ROOT%YourIcon.ico"
```

---

## 配置文件说明

程序配置存储在 exe 同目录下的 JSON 文件中：

```json
{
  "LastFolders": ["D:\\Downloads", "D:\\Documents"],
  "IncludeSubdirectories": true,
  "MinFileSize": 1024,
  "KeepStrategy": 0,
  "DeleteMode": 0,
  "HashVerifyLikelyGroups": false
}
```

**配置项说明：**
- `LastFolders`：上次扫描的文件夹列表
- `IncludeSubdirectories`：是否包含子目录
- `MinFileSize`：最小文件大小（字节）
- `KeepStrategy`：保留策略（0=最旧，1=最新，2=路径最短）
- `DeleteMode`：删除模式（0=回收站，1=永久删除）
- `HashVerifyLikelyGroups`：是否对高置信度组也做哈希验证

---

## 版本历史

### v2.1.0-preview (当前开发版本)
- 可选硬件加速模式与 provider/fallback 架构
- NVIDIA 环境探测与哈希 benchmark
- GPU experimental 无可用 provider 时保持 CPU 完整哈希 fallback

### v2.0.0
- 现代 WinForms v2 首屏布局
- 回收站模式失败时不再静默永久删除
- MD5 精确验证改为全量哈希
- 自测程序默认生成临时夹具
- 新增开源发布文档、CI、发布打包脚本

### v1.2.0
- ✅ 修复删除权限问题（多层删除策略）
- ✅ 修复 TreeView 自定义复选框显示
- ✅ 优化内存使用（限制节点显示数量）
- ✅ 改进界面交互（三种选择状态）

### v1.1.0
- ✅ 添加 MD5 精确验证功能
- ✅ 改进重复检测算法
- ✅ 优化大文件处理性能

### v1.0.0
- ✅ 基础重复文件检测功能
- ✅ 三种智能标记策略
- ✅ 回收站/永久删除模式
- ✅ 绿色便携设计

---

## 开发者信息

### 技术栈
- 语言：C# 5.0
- 框架：.NET Framework 4.8
- UI：WinForms（原生）
- 编译器：csc.exe（系统自带）

### 开发原则
- **绿色便携**：无需安装，不写注册表
- **兼容性强**：使用旧语法，支持 Windows 7+
- **数据正确性优先**：精确验证使用全量分块 MD5，避免抽样误判
- **用户友好**：详细错误提示，安全删除机制

---

## 许可说明

本项目为绿色免费软件，可自由使用、修改和分发。

---

**最后更新：2026-06-19**
**版本：v2.1.0-preview**
