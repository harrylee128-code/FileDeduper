#include <cuda_runtime.h>

#include <chrono>
#include <cstdio>
#include <cstdint>
#include <cstring>
#include <cwchar>
#include <vector>

static const size_t CHUNK_SIZE = 4 * 1024 * 1024;

static void copy_reason(wchar_t* reason, int reasonLength, const wchar_t* message)
{
    if (reason == nullptr || reasonLength <= 0) return;
    if (message == nullptr) message = L"";
    wcsncpy_s(reason, reasonLength, message, _TRUNCATE);
}

static void copy_reason_ascii(wchar_t* reason, int reasonLength, const char* message)
{
    if (reason == nullptr || reasonLength <= 0) return;
    if (message == nullptr) message = "";
    size_t converted = 0;
    mbstowcs_s(&converted, reason, reasonLength, message, _TRUNCATE);
}

__device__ static uint32_t left_rotate(uint32_t x, uint32_t c)
{
    return (x << c) | (x >> (32 - c));
}

__device__ static uint32_t load_le32(const unsigned char* p)
{
    return ((uint32_t)p[0])
        | ((uint32_t)p[1] << 8)
        | ((uint32_t)p[2] << 16)
        | ((uint32_t)p[3] << 24);
}

__device__ static void md5_transform(uint32_t state[4], const unsigned char* block)
{
    const uint32_t s[64] = {
        7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
        5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20,
        4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
        6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21
    };
    const uint32_t k[64] = {
        0xd76aa478, 0xe8c7b756, 0x242070db, 0xc1bdceee,
        0xf57c0faf, 0x4787c62a, 0xa8304613, 0xfd469501,
        0x698098d8, 0x8b44f7af, 0xffff5bb1, 0x895cd7be,
        0x6b901122, 0xfd987193, 0xa679438e, 0x49b40821,
        0xf61e2562, 0xc040b340, 0x265e5a51, 0xe9b6c7aa,
        0xd62f105d, 0x02441453, 0xd8a1e681, 0xe7d3fbc8,
        0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed,
        0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,
        0xfffa3942, 0x8771f681, 0x6d9d6122, 0xfde5380c,
        0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
        0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x04881d05,
        0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,
        0xf4292244, 0x432aff97, 0xab9423a7, 0xfc93a039,
        0x655b59c3, 0x8f0ccc92, 0xffeff47d, 0x85845dd1,
        0x6fa87e4f, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1,
        0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391
    };

    uint32_t m[16];
    for (int i = 0; i < 16; ++i) m[i] = load_le32(block + i * 4);

    uint32_t a = state[0];
    uint32_t b = state[1];
    uint32_t c = state[2];
    uint32_t d = state[3];

    for (uint32_t i = 0; i < 64; ++i)
    {
        uint32_t f;
        uint32_t g;
        if (i < 16)
        {
            f = (b & c) | ((~b) & d);
            g = i;
        }
        else if (i < 32)
        {
            f = (d & b) | ((~d) & c);
            g = (5 * i + 1) & 15;
        }
        else if (i < 48)
        {
            f = b ^ c ^ d;
            g = (3 * i + 5) & 15;
        }
        else
        {
            f = c ^ (b | (~d));
            g = (7 * i) & 15;
        }

        uint32_t temp = d;
        d = c;
        c = b;
        b = b + left_rotate(a + f + k[i] + m[g], s[i]);
        a = temp;
    }

    state[0] += a;
    state[1] += b;
    state[2] += c;
    state[3] += d;
}

__global__ static void md5_transform_kernel(const unsigned char* data, size_t blockCount, uint32_t* state)
{
    if (blockIdx.x != 0 || threadIdx.x != 0) return;

    uint32_t local[4] = { state[0], state[1], state[2], state[3] };
    for (size_t i = 0; i < blockCount; ++i)
    {
        md5_transform(local, data + i * 64);
    }
    state[0] = local[0];
    state[1] = local[1];
    state[2] = local[2];
    state[3] = local[3];
}

static int process_blocks(unsigned char* deviceBuffer, uint32_t* deviceState, uint32_t state[4], const unsigned char* data, size_t bytes, wchar_t* reason, int reasonLength)
{
    if (bytes == 0) return 0;
    if ((bytes % 64) != 0)
    {
        copy_reason(reason, reasonLength, L"Internal error: MD5 block byte count is not aligned.");
        return 20;
    }

    cudaError_t err = cudaMemcpy(deviceBuffer, data, bytes, cudaMemcpyHostToDevice);
    if (err != cudaSuccess)
    {
        copy_reason_ascii(reason, reasonLength, cudaGetErrorString(err));
        return 21;
    }
    err = cudaMemcpy(deviceState, state, sizeof(uint32_t) * 4, cudaMemcpyHostToDevice);
    if (err != cudaSuccess)
    {
        copy_reason_ascii(reason, reasonLength, cudaGetErrorString(err));
        return 22;
    }

    md5_transform_kernel<<<1, 1>>>(deviceBuffer, bytes / 64, deviceState);
    err = cudaDeviceSynchronize();
    if (err != cudaSuccess)
    {
        copy_reason_ascii(reason, reasonLength, cudaGetErrorString(err));
        return 23;
    }

    err = cudaMemcpy(state, deviceState, sizeof(uint32_t) * 4, cudaMemcpyDeviceToHost);
    if (err != cudaSuccess)
    {
        copy_reason_ascii(reason, reasonLength, cudaGetErrorString(err));
        return 24;
    }
    return 0;
}

extern "C" __declspec(dllexport) int fd_cuda_is_available(wchar_t* reason, int reasonLength)
{
    int count = 0;
    cudaError_t err = cudaGetDeviceCount(&count);
    if (err != cudaSuccess)
    {
        copy_reason_ascii(reason, reasonLength, cudaGetErrorString(err));
        return 1;
    }
    if (count <= 0)
    {
        copy_reason(reason, reasonLength, L"No CUDA device found.");
        return 2;
    }

    cudaDeviceProp prop;
    err = cudaGetDeviceProperties(&prop, 0);
    if (err != cudaSuccess)
    {
        copy_reason_ascii(reason, reasonLength, cudaGetErrorString(err));
        return 3;
    }
    wchar_t buffer[256];
    swprintf_s(buffer, 256, L"CUDA device available: %S, compute capability %d.%d", prop.name, prop.major, prop.minor);
    copy_reason(reason, reasonLength, buffer);
    return 0;
}

extern "C" __declspec(dllexport) int fd_cuda_md5_file_utf16(
    const wchar_t* path,
    wchar_t* hashHex,
    int hashHexLength,
    wchar_t* reason,
    int reasonLength,
    unsigned long long* bytesRead,
    double* elapsedMs)
{
    if (bytesRead != nullptr) *bytesRead = 0;
    if (elapsedMs != nullptr) *elapsedMs = 0.0;
    if (hashHex != nullptr && hashHexLength > 0) hashHex[0] = 0;

    if (path == nullptr || path[0] == 0)
    {
        copy_reason(reason, reasonLength, L"Path is empty.");
        return 10;
    }

    auto start = std::chrono::high_resolution_clock::now();

    FILE* fp = nullptr;
    errno_t openErr = _wfopen_s(&fp, path, L"rb");
    if (openErr != 0 || fp == nullptr)
    {
        copy_reason(reason, reasonLength, L"Unable to open file.");
        return 11;
    }

    unsigned char* deviceBuffer = nullptr;
    uint32_t* deviceState = nullptr;
    cudaError_t err = cudaSetDevice(0);
    if (err != cudaSuccess)
    {
        fclose(fp);
        copy_reason_ascii(reason, reasonLength, cudaGetErrorString(err));
        return 12;
    }
    err = cudaMalloc((void**)&deviceBuffer, CHUNK_SIZE);
    if (err != cudaSuccess)
    {
        fclose(fp);
        copy_reason_ascii(reason, reasonLength, cudaGetErrorString(err));
        return 13;
    }
    err = cudaMalloc((void**)&deviceState, sizeof(uint32_t) * 4);
    if (err != cudaSuccess)
    {
        cudaFree(deviceBuffer);
        fclose(fp);
        copy_reason_ascii(reason, reasonLength, cudaGetErrorString(err));
        return 14;
    }

    std::vector<unsigned char> buffer(CHUNK_SIZE);
    unsigned char tail[64];
    size_t tailSize = 0;
    uint64_t totalBytes = 0;
    uint32_t state[4] = { 0x67452301, 0xefcdab89, 0x98badcfe, 0x10325476 };

    while (true)
    {
        size_t read = fread(buffer.data(), 1, buffer.size(), fp);
        if (read > 0)
        {
            totalBytes += (uint64_t)read;
            size_t fullBytes = (read / 64) * 64;
            tailSize = read - fullBytes;
            if (tailSize > 0)
            {
                memcpy(tail, buffer.data() + fullBytes, tailSize);
            }

            int code = process_blocks(deviceBuffer, deviceState, state, buffer.data(), fullBytes, reason, reasonLength);
            if (code != 0)
            {
                cudaFree(deviceState);
                cudaFree(deviceBuffer);
                fclose(fp);
                return code;
            }
        }

        if (read < buffer.size())
        {
            if (ferror(fp))
            {
                cudaFree(deviceState);
                cudaFree(deviceBuffer);
                fclose(fp);
                copy_reason(reason, reasonLength, L"File read failed.");
                return 15;
            }
            break;
        }
    }

    unsigned char finalBlocks[128];
    memset(finalBlocks, 0, sizeof(finalBlocks));
    memcpy(finalBlocks, tail, tailSize);
    finalBlocks[tailSize] = 0x80;
    size_t finalBlockCount = tailSize >= 56 ? 2 : 1;
    uint64_t bitLength = totalBytes * 8ULL;
    size_t lengthOffset = finalBlockCount * 64 - 8;
    for (int i = 0; i < 8; ++i)
    {
        finalBlocks[lengthOffset + i] = (unsigned char)((bitLength >> (8 * i)) & 0xff);
    }

    int finalCode = process_blocks(deviceBuffer, deviceState, state, finalBlocks, finalBlockCount * 64, reason, reasonLength);
    cudaFree(deviceState);
    cudaFree(deviceBuffer);
    fclose(fp);
    if (finalCode != 0) return finalCode;

    if (hashHex == nullptr || hashHexLength < 33)
    {
        copy_reason(reason, reasonLength, L"Hash buffer too small.");
        return 16;
    }

    unsigned char digest[16];
    for (int i = 0; i < 4; ++i)
    {
        digest[i * 4 + 0] = (unsigned char)(state[i] & 0xff);
        digest[i * 4 + 1] = (unsigned char)((state[i] >> 8) & 0xff);
        digest[i * 4 + 2] = (unsigned char)((state[i] >> 16) & 0xff);
        digest[i * 4 + 3] = (unsigned char)((state[i] >> 24) & 0xff);
    }

    static const wchar_t* hex = L"0123456789abcdef";
    for (int i = 0; i < 16; ++i)
    {
        hashHex[i * 2] = hex[(digest[i] >> 4) & 0xf];
        hashHex[i * 2 + 1] = hex[digest[i] & 0xf];
    }
    hashHex[32] = 0;

    auto end = std::chrono::high_resolution_clock::now();
    if (bytesRead != nullptr) *bytesRead = (unsigned long long)totalBytes;
    if (elapsedMs != nullptr)
    {
        *elapsedMs = std::chrono::duration<double, std::milli>(end - start).count();
    }
    copy_reason(reason, reasonLength, L"CUDA full-file MD5 completed.");
    return 0;
}
