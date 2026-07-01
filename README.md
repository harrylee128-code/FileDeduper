# FileDeduper

FileDeduper 是一个绿色便携的 Windows 重复文件查找与清理工具。它使用 C# WinForms 和 .NET Framework 编写，不依赖第三方运行时，不写注册表，配置文件保存在程序同目录。

## 功能

- 多文件夹扫描，支持递归扫描子目录。
- 快速查重：按文件大小、文件名、修改时间筛选重复候选。
- 精确验证：可对候选组执行全量 MD5 验证。
- 智能标记：支持保留最旧、最新、路径最短的副本。
- 安全默认：疑似组不会默认标记删除，需要 MD5 验证或手动选择。
- 安全删除：默认移入回收站；如果回收站失败，不会静默永久删除。
- 绿色便携：构建产物可直接复制运行。

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

生成文件位于 `dist\FileDeduper-v2.0.0.zip`。

## 安全说明

这个工具会执行文件删除操作。建议先使用测试文件夹验证结果，再对正式数据执行删除。

默认删除模式是“移入回收站”。如果 Windows 回收站操作失败，程序会报告失败并保留原文件，不会自动改为永久删除。

为了避免网络共享、不可识别盘符、服务/CI 等非交互环境被系统永久删除，程序只会在交互式用户会话、当前进程能访问 Windows Shell 回收站且路径位于本机固定磁盘时执行回收站删除；无法确认时会报告失败并保留文件。

## 开发文档

更多架构、目录结构和扩展说明见 [README-DEV.md](README-DEV.md)。

工程原则与发布检查：

- [ENGINEERING.md](docs/ENGINEERING.md)
- [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md)

## 许可证

MIT License，见 [LICENSE](LICENSE)。
