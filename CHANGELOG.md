# Changelog

## v2.2.0-preview.1 - Unreleased

- 新增“排除关键词”功能：文件夹关键词和文件名关键词分栏配置。
- 文件夹关键词命中目录路径或目录名时跳过整棵目录；文件名关键词只匹配文件名。
- 扫描状态新增排除文件数/目录数，避免误以为漏扫。
- 绿色配置文件保存排除关键词，随 exe 同目录迁移。
- 新增实验性 CUDA native provider：`FileDeduperCuda.dll`，GPU 模式可计算完整文件 MD5。
- 新增 `build-gpu.cmd` 和 `package-gpu-release.cmd`，用于生成 CUDA 预览包。
- 关于窗口运行时显示 `Lite`、`CUDA` 或 `CUDA fallback` 渠道。
- 版本升级到 `v2.2.0-preview.1`，Lite 包名为 `FileDeduper-v2.2.0-preview1-lite.zip`。
- CUDA 包名为 `FileDeduper-v2.2.0-preview1-cuda.zip`。

## v2.1.0-preview.3 - 2026-07-02

- 关于窗口和主窗口标题显示明确版本号与 Lite 包标识，避免桌面多版本混淆。
- README 增加版本更新记录，汇总各版本主要新增、修复和安全变化。
- 打包版本升为 `FileDeduper-v2.1.0-preview3-lite.zip`。

## v2.1.0-preview.2 - 2026-07-02

- 新增可选硬件加速模式、NVIDIA 环境探测、CPU fallback 和哈希 benchmark 入口。
- 新增可配置哈希并行度；Auto 模式保守使用最多 4 个并发文件哈希，支持手动设为 1 单线程。
- benchmark 增加 CPU sequential、CPU auto parallel、GPU experimental fallback 对比输出。
- 新增 GPU/硬件加速边界说明和后续路线图。
- 记录未来 GitHub Release 拆分轻量版与 GPU 支持版的发布策略。

## v2.0.0 - 2026-07-01

- 准备现代 WinForms v2 开源版本。
- 修复回收站模式下可能静默永久删除文件的高风险问题。
- 对 UNC/network、CI/automation、非交互会话、无法访问 Shell 回收站等无法确认支持本机回收站的场景采用 fail-closed 处理。
- 修复大文件 MD5 精确验证可能因抽样哈希误判的问题。
- 删除缺失文件现在会报告失败，不再静默忽略。
- 扫描器改为安全递归枚举，单个目录失败不会中断整个扫描。
- 疑似组默认不再自动标记删除，降低同大小文件误删风险。
- 自测程序改为默认生成临时夹具，便于开源后复现。
- 新增开源仓库基础文件和安全说明。

## v1.2.0

- 修复删除权限问题。
- 修复 TreeView 自定义复选框显示。
- 限制结果节点数量，降低内存风险。
- 改进三态选择交互。
