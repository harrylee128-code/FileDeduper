# Release Checklist

Use this before publishing a GitHub release.

## Build and Test

- [ ] `build.cmd` succeeds.
- [ ] `build-test.cmd` succeeds.
- [ ] `bin\Test\FileDeduper.Test.exe` reports `ALL PASSED`.
- [ ] `package-release.cmd` creates `dist\FileDeduper-v2.1.0-preview2-lite.zip`.
- [ ] GitHub Actions Build workflow passes on the release commit and uploads the portable zip artifact.
- [ ] Recycle Bin mode is manually verified with disposable files.
- [ ] Permanent delete mode is verified only with disposable files.

## Safety Review

- [ ] Recycle Bin failures do not fall back to permanent delete.
- [ ] Suspected groups are not auto-marked for deletion after scan.
- [ ] Delete confirmation shows file count, total size, delete mode, and sample paths.
- [ ] Failed deletes remain visible to the user.
- [ ] Hash verification labels are not misleading.
- [ ] Cancellation paths restore enabled/disabled UI state.

## Open Source Hygiene

- [ ] `README.md`, `LICENSE`, `SECURITY.md`, `CONTRIBUTING.md`, and `CHANGELOG.md` are current.
- [ ] `bin/` and generated config files are not committed.
- [ ] No private local paths are required to build or test.
- [ ] Issue templates are present.

## Hardware Acceleration Releases

- [ ] Lite/portable asset remains available and requires no GPU runtime.
- [ ] GPU-enabled asset, if published, documents supported GPU vendors, driver/runtime requirements, fallback behavior, and third-party licenses.
- [ ] GPU-enabled asset does not replace or weaken CPU full-file hash correctness.

## Manual UI Pass

- [ ] Main window has no overlapping controls at minimum size.
- [ ] Empty, scanning, results, verification, partial failure, and delete-complete states are readable.
- [ ] Disabled destructive actions are visually subdued.
