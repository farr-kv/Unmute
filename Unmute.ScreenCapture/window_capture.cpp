// THIS IS CLANKER CODE. RIP

#include "pch.h"
#include "screen_capture.h"

#include <d3d11.h>
#include <dxgi1_2.h>

#pragma comment(lib, "d3d11.lib")
#pragma comment(lib, "dxgi.lib")

static ID3D11Device* gDevice = nullptr;
static ID3D11DeviceContext* gContext = nullptr;
static IDXGIOutputDuplication* gDuplication = nullptr;
static ID3D11Texture2D* gStaging = nullptr;

static UINT gWidth = 0;
static UINT gHeight = 0;

bool InitializeCapture()
{
    if (gDuplication)
        return true;

    HRESULT hr;

    D3D_FEATURE_LEVEL level;

    hr = D3D11CreateDevice(
        nullptr,
        D3D_DRIVER_TYPE_HARDWARE,
        nullptr,
        0,
        nullptr,
        0,
        D3D11_SDK_VERSION,
        &gDevice,
        &level,
        &gContext);

    if (FAILED(hr))
        return false;

    IDXGIDevice* dxgiDevice = nullptr;
    hr = gDevice->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgiDevice);
    if (FAILED(hr))
        return false;

    IDXGIAdapter* adapter = nullptr;
    dxgiDevice->GetAdapter(&adapter);

    IDXGIOutput* output = nullptr;
    adapter->EnumOutputs(0, &output);

    IDXGIOutput1* output1 = nullptr;
    output->QueryInterface(__uuidof(IDXGIOutput1), (void**)&output1);

    hr = output1->DuplicateOutput(gDevice, &gDuplication);

    output1->Release();
    output->Release();
    adapter->Release();
    dxgiDevice->Release();

    if (FAILED(hr))
        return false;

    DXGI_OUTDUPL_DESC dupDesc;
    gDuplication->GetDesc(&dupDesc);

    gWidth = dupDesc.ModeDesc.Width;
    gHeight = dupDesc.ModeDesc.Height;

    D3D11_TEXTURE2D_DESC desc = {};

    desc.Width = gWidth;
    desc.Height = gHeight;
    desc.ArraySize = 1;
    desc.MipLevels = 1;
    desc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
    desc.SampleDesc.Count = 1;
    desc.Usage = D3D11_USAGE_STAGING;
    desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;

    hr = gDevice->CreateTexture2D(&desc, nullptr, &gStaging);

    return SUCCEEDED(hr);
}

void ShutdownCapture()
{
    if (gStaging)
    {
        gStaging->Release();
        gStaging = nullptr;
    }

    if (gDuplication)
    {
        gDuplication->Release();
        gDuplication = nullptr;
    }

    if (gContext)
    {
        gContext->Release();
        gContext = nullptr;
    }

    if (gDevice)
    {
        gDevice->Release();
        gDevice = nullptr;
    }
}

bool GetCaptureSize(int* width, int* height)
{
    if (!gDuplication)
        return false;

    *width = (int)gWidth;
    *height = (int)gHeight;

    return true;
}

bool CaptureFrame(
    uint8_t* destination,
    int destinationSize,
    int* width,
    int* height)
{
    if (!gDuplication)
        return false;

    const int required = gWidth * gHeight * 4;

    if (destinationSize < required)
        return false;

    DXGI_OUTDUPL_FRAME_INFO frameInfo;
    IDXGIResource* resource = nullptr;

    HRESULT hr = gDuplication->AcquireNextFrame(
        100,
        &frameInfo,
        &resource);

    if (FAILED(hr))
        return false;

    ID3D11Texture2D* frame = nullptr;
    resource->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&frame);

    gContext->CopyResource(gStaging, frame);

    D3D11_MAPPED_SUBRESOURCE mapped;

    hr = gContext->Map(
        gStaging,
        0,
        D3D11_MAP_READ,
        0,
        &mapped);

    if (FAILED(hr))
    {
        frame->Release();
        resource->Release();
        gDuplication->ReleaseFrame();
        return false;
    }

    for (UINT y = 0; y < gHeight; y++)
    {
        memcpy(
            destination + y * gWidth * 4,
            (uint8_t*)mapped.pData + y * mapped.RowPitch,
            gWidth * 4);
    }

    gContext->Unmap(gStaging, 0);

    frame->Release();
    resource->Release();

    gDuplication->ReleaseFrame();

    *width = gWidth;
    *height = gHeight;

    return true;
}