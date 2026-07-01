# Contributing

Thanks for helping improve FileDeduper.

## Development Setup

Use a Windows machine with .NET Framework compiler available at:

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

Build and test before submitting changes:

```cmd
build.cmd
build-test.cmd
bin\Test\FileDeduper.Test.exe
```

## Contribution Guidelines

- Follow [ENGINEERING.md](docs/ENGINEERING.md): first principles first, adversarial review before release.
- Treat deletion behavior as safety-critical.
- Add or update self-tests for scan, duplicate detection, hashing, marking, and deletion behavior.
- Keep the app portable: do not add registry writes, installers, or mandatory third-party runtimes without discussion.
- Keep UI labels user-facing and avoid exposing internal implementation details.
- Do not commit generated `bin/` output or local config files.
