# Engineering Principles

FileDeduper touches user files, so development must start from safety and evidence.

## First Principles

1. A selected Recycle Bin delete must never silently become a permanent delete.
2. A duplicate result must be explainable: why files are grouped, which file is kept, and what will be deleted.
3. A failed operation must leave files in the safest recoverable state and report the failure.
4. Suspected duplicates are candidates, not deletion targets, until verified or explicitly selected.
5. A fresh contributor must be able to build and test without private local folders or paid tools.
6. UI polish must support safer decisions, not hide risk behind decoration.

## Adversarial Review Checklist

Before accepting a change, review it as if it will fail in the worst practical way:

- Deletion: Can this path delete more files than the user selected? Can Recycle mode bypass the Recycle Bin?
- Recycle Bin API flags: Are both undo/recycle behavior and permanent-delete warning behavior preserved?
- Detection: Can unrelated same-size files be presented as verified duplicates?
- Cancellation: Can cancel leave stale progress, stale selections, or half-updated state?
- Errors: Are access-denied, locked files, missing folders, and malformed config handled without crashing?
- UI: Is the delete count, reclaimable size, and delete mode visible before destructive action?
- Portability: Does this require local-only paths, SDKs, generated binaries, or private config?
- Tests: Does a test prove the behavior with disposable files?

## Implementation Rules

- Add or update self-tests for any safety-critical behavior.
- Keep permanent delete behind explicit user selection and confirmation.
- Prefer small, verifiable changes over broad rewrites.
- Do not add external dependencies unless they materially improve safety, build reproducibility, or maintainability.
- Treat UI changes as product changes: verify screenshots for overlap, disabled states, and dangerous-action affordance.
