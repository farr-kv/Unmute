#pragma once

extern "C" __declspec(dllexport) bool __stdcall CaptureWindowToBuffer(HWND hwnd, BYTE** outBuffer, UINT32* outSize);
extern "C" __declspec(dllexport) void __stdcall FreeCaptureBuffer(BYTE* buffer);