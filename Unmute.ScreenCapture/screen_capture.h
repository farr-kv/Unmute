#pragma once

#include <Windows.h>
#include <stdint.h>

#ifdef DXGICAPTURE_EXPORTS
#define DXGI_API __declspec(dllexport)
#else
#define DXGI_API __declspec(dllimport)
#endif

extern "C"
{
    DXGI_API bool InitializeCapture();

    DXGI_API void ShutdownCapture();

    DXGI_API bool CaptureFrame(
        uint8_t* destination,
        int destinationSize,
        int* width,
        int* height);

    DXGI_API bool GetCaptureSize(
        int* width,
        int* height);
}