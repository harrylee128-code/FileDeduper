# Roadmap

This project prioritizes safe local duplicate-file cleanup before broader media intelligence.

## v2.x

- Improve UI accessibility and keyboard workflows.
- Add safer preview/export reports before deletion.
- Add optional dry-run mode for large folders.
- Add more deletion and permission edge-case tests.
- Expand hardware acceleration provider support after the optional CPU/GPU provider boundary is stable.

## Later

- Similar-image detection using perceptual hashes.
- Video/audio fingerprinting for media libraries.
- Optional CUDA/DirectML/OpenCL/OpenVINO acceleration for compute-heavy file and media analysis.

## Release Packaging Strategy

Future GitHub releases should publish two asset families:

- **Lite / Portable**: no GPU dependency, CPU full-file hashing, safest default for all Windows users.
- **GPU-enabled**: optional CUDA/Intel/DirectML provider package with explicit hardware, driver, runtime, and license notes.

The Lite build remains the baseline and must always pass CI on a no-GPU GitHub Actions runner. GPU-enabled builds must keep CPU fallback and must not weaken verified-duplicate correctness.

GPU acceleration should stay optional. CUDA, OpenCL, DirectML, OpenVINO, or ONNX Runtime GPU must not be required for the basic portable duplicate-file workflow.
