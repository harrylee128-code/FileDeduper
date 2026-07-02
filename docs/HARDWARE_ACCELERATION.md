# Hardware Acceleration

FileDeduper targets very large local collections, including hundreds of GB or TB-scale folders. Hardware acceleration is useful only when it improves the real end-to-end pipeline without weakening correctness or portability.

## First Principles

Duplicate cleanup has several separate bottlenecks:

1. Directory enumeration and metadata reads.
2. Disk throughput while reading file content.
3. Hash computation.
4. Grouping and UI rendering.
5. Deletion safety and Recycle Bin behavior.

For normal duplicate verification, disk I/O is often the limiting factor. A GPU can hash bytes quickly, but the application still has to read those bytes from storage and transfer work to the accelerator. GPU acceleration only helps when file reads are fast enough and the hash workload is large enough to amortize provider startup and transfer overhead.

## Current v2.x Behavior

- CPU full-file MD5 remains the correctness baseline.
- Hardware acceleration is optional and controlled by `HardwareAccelerationMode`.
- `Auto` uses the safe available provider.
- `CpuOnly` forces the portable CPU implementation.
- `GpuExperimental` probes the environment and falls back to CPU if no redistributable native provider is available.
- Hash verification can process multiple files in the same candidate group concurrently. `HashParallelism = 0` means Auto, currently capped at 4 concurrent files; `1` forces sequential hashing.
- The CUDA preview package includes `FileDeduperCuda.dll`, an optional native provider that computes full-file MD5 through CUDA and falls back to CPU on load or runtime failure.

## CUDA / Intel / DirectML Direction

CUDA is valuable for NVIDIA-only installations, but it requires a native provider and a build/distribution story. This repository should not require CUDA Toolkit, Visual Studio C++ workloads, or proprietary binaries for the base app.

Intel acceleration should be evaluated through DirectML, OpenCL, OpenVINO, or ONNX Runtime GPU only as optional modules. These providers must:

- keep CPU fallback available;
- never downgrade verified duplicates to sampled or approximate hashes;
- pass the same correctness tests as CPU;
- document runtime and license requirements;
- remain disabled by default until benchmarked.

## Release Packaging

GitHub releases should keep a lightweight portable asset and a GPU-enabled asset separate:

- `FileDeduper-vX.Y.Z-lite.zip`: CPU-only baseline, no CUDA/Intel runtime required.
- `FileDeduper-vX.Y.Z-gpu.zip`: optional native provider package for users who explicitly want GPU acceleration.

This split keeps the app usable for ordinary users while allowing TB-scale users to opt into CUDA/Intel dependencies knowingly.

## Benchmarking

Run:

```cmd
build-test.cmd
bin\Test\FileDeduper.Test.exe --benchmark <folder>
```

The benchmark reports file count, bytes processed, provider name, requested/effective parallelism, hardware acceleration state, elapsed time, and MB/s. Use disposable data first. For real TB-scale validation, run on the target GPU machine and compare CPU sequential, CPU Auto parallel, and CUDA experimental. Do not assume CUDA is faster; the v1 provider is correctness-first.
