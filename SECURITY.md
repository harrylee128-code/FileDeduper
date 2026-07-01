# Security Policy

## Supported Versions

Only the latest source version is maintained.

## Reporting a Vulnerability

If you find a security or data-loss issue, please open a GitHub issue with:

- affected version or commit;
- exact reproduction steps;
- expected and actual behavior;
- whether files were moved to Recycle Bin, permanently deleted, or left unchanged.

For file-deletion bugs, include a minimal disposable test folder layout. Do not attach private files.

## Safety Guarantees

When the delete mode is set to Recycle Bin, FileDeduper must not silently fall back to permanent deletion. If Recycle Bin deletion fails, the file should remain in place and the failure should be visible to the user.

Paths that are not known to support the local Windows Recycle Bin, such as UNC/network paths, should fail closed before the delete API is called.

Recycle Bin calls should include the Windows Shell permanent-delete warning flag so the OS can interrupt cases where a delete would be destroyed rather than recycled.
