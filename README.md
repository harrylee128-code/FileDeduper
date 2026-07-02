# FileDeduper

当前版本：`v2.1.0-preview.3` (`Lite`)

FileDeduper 是一个绿色便携的 Windows 重复文件查找与清理工具。它使用 C# WinForms 和 .NET Framework 编写，不依赖第三方运行时，不写注册表，配置文件保存在程序同目录。

## 功能

- 多文件夹扫描，支持递归扫描子目录。
- 快速查重：按文件大小、文件名、修改时间筛选重复候选。
- 精确验证：可对候选组执行全量 MD5 验证。
- 智能标记：支持保留最旧、最新、路径最短的副本。
- 安全默认：疑似组不会默认标记删除，需要 MD5 验证或手动选择。
- 安全删除：默认移入回收站；如果回收站失败，不会静默永久删除。
- 绿色便携：构建产物可直接复制运行。

## GPU / Hardware Acceleration

当前 v2 版本加入了可选硬件加速模式和 benchmark 基线，但不把 CUDA、OpenCL 或 Intel runtime 作为基础依赖。重复文件查找的主要瓶颈通常是磁盘 I/O 和安全文件操作，而不是 MD5 计算；引入 GPU 运行时也会破坏绿色便携和低依赖目标。

`GpuExperimental` 会探测可见的 NVIDIA 环境，但没有可再分发 native provider 时会自动回退 CPU，不影响完整哈希正确性。

设置里的“哈希并行度”用于同一候选组内多文件并行读取与哈希：`0` 表示 Auto，当前最多使用 4 个并发文件；`1` 表示强制单线程。机械硬盘、网络盘或 NAS 上建议先用 1 或 Auto 小样本测试。

运行哈希 benchmark：

```cmd
build-test.cmd
bin\Test\FileDeduper.Test.exe --benchmark <folder>
```

后续如果增加图片相似度、视频指纹或 AI 内容识别等计算密集功能，可以考虑以可选插件方式接入 CUDA、DirectML、OpenCL、OpenVINO 或 ONNX Runtime GPU，不影响基础查重功能。

发布时会保留两个方向：轻量绿色版（无 GPU 依赖）和 GPU 支持版（单独说明 CUDA/Intel/DirectML 运行时要求），用户按机器条件选择。

## 下载与运行

当前仓库以源码发布为主。构建后运行：

```cmd
bin\Release\FileDeduper.exe
```

运行环境：

- Windows 10/11
- .NET Framework 4.x 运行时

## 构建

本项目使用系统自带的 .NET Framework 编译器，不需要 Visual Studio 或 .NET SDK。

```cmd
build.cmd
```

测试构建：

```cmd
build-test.cmd
bin\Test\FileDeduper.Test.exe
```

测试程序默认会创建临时测试数据，不依赖本机私有目录。

GitHub Actions 会在 Windows runner 上执行同样的构建与自测。

打包绿色版：

```cmd
package-release.cmd
```

生成文件位于 `dist\FileDeduper-v2.1.0-preview3-lite.zip`。

## 版本更新记录

### v2.1.0-preview.3

- 主窗口标题和“关于”窗口显示明确版本号与 `Lite` 包标识。
- README 增加版本更新/修复记录，便于区分桌面上的不同测试包。
- 打包产物升级为 `FileDeduper-v2.1.0-preview3-lite.zip`。

### v2.1.0-preview.2

- 新增可选硬件加速模式、NVIDIA 环境探测、CPU fallback 和哈希 benchmark。
- 新增哈希并行度设置；Auto 模式最多 4 个并发文件哈希，也可手动设为 1 单线程。
- benchmark 输出 CPU sequential、CPU auto parallel、GPU experimental fallback 对比。
- 明确 GitHub Release 后续拆分轻量版和 GPU 支持版。

### v2.0.0

- 修复回收站模式可能静默永久删除文件的高风险问题。
- 回收站失败、UNC/network、CI/automation、非交互会话等场景 fail-closed，不回退永久删除。
- MD5 精确验证改为完整文件读取，避免抽样哈希误判。
- 疑似组默认不自动标记删除，降低误删风险。
- 新增开源发布基础文件、CI、自测夹具和绿色打包脚本。

### v1.2.0

- 修复删除权限问题。
- 修复 TreeView 自定义复选框显示。
- 限制结果节点数量，降低内存风险。
- 改进三态选择交互。

## 安全说明

这个工具会执行文件删除操作。建议先使用测试文件夹验证结果，再对正式数据执行删除。

默认删除模式是“移入回收站”。如果 Windows 回收站操作失败，程序会报告失败并保留原文件，不会自动改为永久删除。

为了避免网络共享、不可识别盘符、CI/服务等自动化环境被系统永久删除，程序只会在普通交互式用户会话、当前进程能访问 Windows Shell 回收站且路径位于本机固定磁盘时执行回收站删除；无法确认时会报告失败并保留文件。

## 开发文档

更多架构、目录结构和扩展说明见 [README-DEV.md](README-DEV.md)。

工程原则与发布检查：

- [ENGINEERING.md](docs/ENGINEERING.md)
- [HARDWARE_ACCELERATION.md](docs/HARDWARE_ACCELERATION.md)
- [ROADMAP.md](docs/ROADMAP.md)
- [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md)

## 许可证

MIT License，见 [LICENSE](LICENSE)。
