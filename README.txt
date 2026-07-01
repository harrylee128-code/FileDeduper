================================================================================
                FileDeduper  ·  文件查重清理助手 v2.0
================================================================================

【快速开始】
1. 双击 bin\Release\FileDeduper.exe 即可启动（无需安装）
2. 添加要扫描的文件夹，勾选是否包含子目录
3. 点击"开始扫描"，查看重复文件结果
4. 选择保留策略（保留最旧/最新/路径最短），点击"自动标记"
5. 勾选或反选要删除的文件，点击"删除选中"


【核心功能】
✓ 多文件夹扫描：可添加多个文件夹一起扫描
✓ 两阶段查重：快速预筛（大小/名称/时间） + 可选全量 MD5 精确验证
✓ 智能标记：三种保留策略，自动挑选保留原件，标记高置信重复副本
✓ 安全默认：疑似组不会默认标记删除，需要 MD5 验证或手动选择
✓ 安全删除：默认移入回收站（可恢复），可选永久删除
✓ 绿色便携：配置文件随 exe 同目录，不写注册表，可随意拷贝使用


【编译说明】
本程序使用 .NET Framework 4.8 自带的 csc.exe 编译，无需 SDK。

编译主程序 (GUI)：
  cd FileDeduper
  build.cmd

编译自测程序 (控制台测试)：
  cd FileDeduper
  build-test.cmd

依赖：
  - Win10/11 自带的 .NET Framework 4.8 运行时（无需安装）
  - 无需额外运行时或 DLL


【自测验证】
运行自测程序验证核心功能：
  bin\Test\FileDeduper.Test.exe

会自动扫描 TestDedupe 测试文件夹，验证：
  ✓ 扫描能找出文件
  ✓ 快速预筛能正确分组（Likely 完全重复、Suspected 疑似）
  ✓ MD5 精确验证能升格为 Verified
  ✓ 三种智能标记策略都能正确保留一个、标记其余
  ✓ 永久删除功能正常
  ✓ 配置读写正常


【技术架构】
- 框架：.NET Framework 4.8 + WinForms
- 语言：C# 5（兼容 Win7+ 自带的旧编译器）
- 架构：分层设计 (Models → Utils → Core → Forms)
  * Models：FileEntry, DuplicateGroup, 枚举, AppSettings
  * Utils：ConfigStore (JSON), RecycleBinHelper (P/Invoke), HashHelper (MD5)
  * Core：FileScanner (后台扫描), DuplicateDetector (两阶段查重),
          SmartMarker (智能标记), FileDeleter (删除)
  * Forms：MainForm (主界面), SettingsForm (设置弹窗)
- 绿色化：配置存 exe 同目录，不写 AppData/注册表


【设置选项】
设置弹窗（界面右上角"设置"）：
- 默认删除方式：移入回收站 / 永久删除
- 默认保留策略：保留最旧 / 最新 / 路径最短
- 是否包含子目录
- 是否对完全重复组也做 MD5 复核（更慢但更准确）
- 最小文件大小：忽略小于此大小的文件


【注意事项】
- 删除操作不可逆（永久删除模式会二次确认）
- 大文件哈希计算需要时间，可随时取消
- 配置文件损坏会自动回退默认配置
- 建议先在测试数据上试运行再对正式数据操作


【构建产物】
bin\Release\FileDeduper.exe        —— 主程序 (GUI, 47KB)
bin\Test\FileDeduper.Test.exe      —— 自测程序 (控制台, 56KB)
FileDeduper.config.json            —— 配置文件（运行时自动生成）


================================================================================
