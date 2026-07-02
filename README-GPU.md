# FileDeduper CUDA Preview

This package includes the optional `FileDeduperCuda.dll` native provider.

Requirements:

- NVIDIA GPU with CUDA support.
- NVIDIA driver.
- CUDA Toolkit or CUDA Runtime 13.3 available on the system DLL search path.

Behavior:

- `GPU experimental` uses the CUDA provider only when the DLL and CUDA runtime load successfully.
- If CUDA is unavailable, FileDeduper falls back to CPU full-file MD5 and reports the reason.
- The CUDA provider computes full-file MD5. It does not use sampling or approximate hashes.
- This preview is correctness-first and may be slower than CPU on some storage layouts.
- On the initial GTX 1660 disposable fixture, CUDA proved correctness but was slower than CPU auto parallel hashing. Treat CUDA as experimental until benchmarked on the target data set.

Run benchmark:

```cmd
bin\Test\FileDeduper.Test.exe --benchmark <folder>
```
