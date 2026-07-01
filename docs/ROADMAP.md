# Roadmap

This project prioritizes safe local duplicate-file cleanup before broader media intelligence.

## v2.x

- Improve UI accessibility and keyboard workflows.
- Add safer preview/export reports before deletion.
- Add optional dry-run mode for large folders.
- Add more deletion and permission edge-case tests.

## Later

- Similar-image detection using perceptual hashes.
- Video/audio fingerprinting for media libraries.
- Optional hardware acceleration for compute-heavy media analysis.

GPU acceleration should stay optional. CUDA, OpenCL, DirectML, OpenVINO, or ONNX Runtime GPU must not be required for the basic portable duplicate-file workflow.
