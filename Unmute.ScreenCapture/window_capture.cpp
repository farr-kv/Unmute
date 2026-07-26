// THIS IS CLANKER CODE. RIP

#include "pch.h"
#include "window_capture.h"

using namespace winrt;
using namespace winrt::Windows::Graphics::Capture;
using namespace winrt::Windows::Graphics::DirectX;
using namespace winrt::Windows::Graphics::DirectX::Direct3D11;

// ---- 1. Get a GraphicsCaptureItem for an HWND (no picker dialog needed) ----
static GraphicsCaptureItem CreateCaptureItemForWindow(HWND hwnd)
{
    auto factory = get_activation_factory<GraphicsCaptureItem>();
    auto interop = factory.as<IGraphicsCaptureItemInterop>();
    GraphicsCaptureItem item{ nullptr };
    check_hresult(interop->CreateForWindow(hwnd, guid_of<GraphicsCaptureItem>(), put_abi(item)));
    return item;
}

// ---- 2. D3D11 device + WinRT IDirect3DDevice wrapper ----
static com_ptr<ID3D11Device> CreateD3DDevice()
{
    com_ptr<ID3D11Device> device;
    D3D_FEATURE_LEVEL fl;
    check_hresult(D3D11CreateDevice(
        nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr,
        D3D11_CREATE_DEVICE_BGRA_SUPPORT,
        nullptr, 0, D3D11_SDK_VERSION,
        device.put(), &fl, nullptr));
    return device;
}

static IDirect3DDevice CreateDirect3DDevice(com_ptr<ID3D11Device> const& d3dDevice)
{
    auto dxgiDevice = d3dDevice.as<IDXGIDevice>();
    com_ptr<::IInspectable> inspectable;
    check_hresult(CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.get(), inspectable.put()));
    return inspectable.as<IDirect3DDevice>();
}

// ---- 3. Grab exactly one frame, synchronously ----
static com_ptr<ID3D11Texture2D> CaptureOneFrame(
    HWND hwnd, com_ptr<ID3D11Device> const& d3dDevice, IDirect3DDevice const& device,
    winrt::Windows::Graphics::SizeInt32& outSize)
{
    auto item = CreateCaptureItemForWindow(hwnd);
    auto size = item.Size();
    outSize = size;

    HANDLE frameEvent = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    com_ptr<ID3D11Texture2D> result;

    auto framePool = Direct3D11CaptureFramePool::CreateFreeThreaded(
        device, DirectXPixelFormat::B8G8R8A8UIntNormalized, 1, size);

    auto session = framePool.CreateCaptureSession(item);

    framePool.FrameArrived([&](auto& sender, auto&)
        {
            if (auto frame = sender.TryGetNextFrame())
            {
                auto access = frame.Surface().as<
                    ::Windows::Graphics::DirectX::Direct3D11::IDirect3DDxgiInterfaceAccess>();
                com_ptr<ID3D11Texture2D> tex;
                access->GetInterface(guid_of<ID3D11Texture2D>(), tex.put_void());
                result = tex;
                SetEvent(frameEvent);
            }
        });

    session.StartCapture();
    WaitForSingleObject(frameEvent, 5000); // 5s timeout is plenty for a single frame
    CloseHandle(frameEvent);
    session.Close();
    framePool.Close();

    return result;
}

// ---- 4. Encode the GPU texture to an in-memory PNG buffer via WIC ----
static bool EncodeTextureToBuffer(com_ptr<ID3D11Device> const& d3dDevice, com_ptr<ID3D11Texture2D> const& texture, BYTE** outBuffer, UINT32* outSize)
{
    if (!texture) return false;

    D3D11_TEXTURE2D_DESC desc;
    texture->GetDesc(&desc);

    D3D11_TEXTURE2D_DESC stagingDesc = desc;
    stagingDesc.Usage = D3D11_USAGE_STAGING;
    stagingDesc.BindFlags = 0;
    stagingDesc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
    stagingDesc.MiscFlags = 0;

    com_ptr<ID3D11Texture2D> staging;
    check_hresult(d3dDevice->CreateTexture2D(&stagingDesc, nullptr, staging.put()));

    com_ptr<ID3D11DeviceContext> ctx;
    d3dDevice->GetImmediateContext(ctx.put());
    ctx->CopyResource(staging.get(), texture.get());

    D3D11_MAPPED_SUBRESOURCE mapped;
    check_hresult(ctx->Map(staging.get(), 0, D3D11_MAP_READ, 0, &mapped));

    com_ptr<IWICImagingFactory> wic;
    check_hresult(CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER,
        IID_PPV_ARGS(wic.put())));

    // In-memory stream instead of a file stream
    com_ptr<IStream> stream;
    check_hresult(CreateStreamOnHGlobal(nullptr, TRUE, stream.put()));

    com_ptr<IWICBitmapEncoder> encoder;
    check_hresult(wic->CreateEncoder(GUID_ContainerFormatPng, nullptr, encoder.put()));
    check_hresult(encoder->Initialize(stream.get(), WICBitmapEncoderNoCache));

    com_ptr<IWICBitmapFrameEncode> frame;
    check_hresult(encoder->CreateNewFrame(frame.put(), nullptr));
    check_hresult(frame->Initialize(nullptr));
    check_hresult(frame->SetSize(desc.Width, desc.Height));
    WICPixelFormatGUID format = GUID_WICPixelFormat32bppBGRA;
    check_hresult(frame->SetPixelFormat(&format));
    check_hresult(frame->WritePixels(desc.Height, mapped.RowPitch, mapped.RowPitch * desc.Height, static_cast<BYTE*>(mapped.pData)));

    ctx->Unmap(staging.get(), 0);

    check_hresult(frame->Commit());
    check_hresult(encoder->Commit());

    // Pull the encoded PNG bytes out of the HGLOBAL backing the stream
    HGLOBAL hGlobal = nullptr;
    check_hresult(GetHGlobalFromStream(stream.get(), &hGlobal));

    SIZE_T size = GlobalSize(hGlobal);
    void* src = GlobalLock(hGlobal);
    if (!src) return false;

    BYTE* dest = static_cast<BYTE*>(CoTaskMemAlloc(size));
    if (!dest)
    {
        GlobalUnlock(hGlobal);
        return false;
    }
    memcpy(dest, src, size);
    GlobalUnlock(hGlobal);

    *outBuffer = dest;
    *outSize = static_cast<UINT32>(size);
    return true;
}

// ---- Exported entry points ----
extern "C" __declspec(dllexport) bool __stdcall CaptureWindowToBuffer(HWND hwnd, BYTE** outBuffer, UINT32* outSize)
{
    if (outBuffer == nullptr || outSize == nullptr) return false;
    *outBuffer = nullptr;
    *outSize = 0;

    try
    {
        HRESULT hr = CoInitializeEx(nullptr, COINIT_MULTITHREADED);
        bool needUninit = SUCCEEDED(hr);

        auto d3dDevice = CreateD3DDevice();
        auto device = CreateDirect3DDevice(d3dDevice);

        winrt::Windows::Graphics::SizeInt32 size;
        auto texture = CaptureOneFrame(hwnd, d3dDevice, device, size);
        bool ok = EncodeTextureToBuffer(d3dDevice, texture, outBuffer, outSize);

        if (needUninit) CoUninitialize();
        return ok;
    }
    catch (...)
    {
        return false;
    }
}

extern "C" __declspec(dllexport) void __stdcall FreeCaptureBuffer(BYTE* buffer)
{
    if (buffer) CoTaskMemFree(buffer);
}
