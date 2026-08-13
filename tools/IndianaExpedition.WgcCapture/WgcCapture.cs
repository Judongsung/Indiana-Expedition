// Portions adapted from Microsoft winapp CLI's WgcCapture implementation.
// Copyright (c) Microsoft Corporation and Contributors. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using IndianaExpedition.WgcCapture.Constants;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Win32;
using Windows.Win32.Foundation;
using D3D = Windows.Win32.Graphics.Direct3D11;
using D3DCommon = Windows.Win32.Graphics.Direct3D;
using DxgiCommon = Windows.Win32.Graphics.Dxgi.Common;
using WinRT;

namespace IndianaExpedition.WgcCapture;

internal static partial class WgcCapture
{
    public static bool IsReportedSupported()
    {
        try
        {
            return GraphicsCaptureSession.IsSupported();
        }
        catch
        {
            return false;
        }
    }

    public static async Task<CaptureFrame> CaptureAsync(HWND windowHandle, CancellationToken cancellationToken)
    {
        // Deliberately perform the real HWND capture even when IsSupported() reports false.
        // There is no PrintWindow, screen-DC, or focus-changing fallback in this tool.
        PInvoke.D3D11CreateDevice(
            pAdapter: null,
            D3DCommon.D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
            Software: default,
            D3D.D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
            pFeatureLevels: default,
            SDKVersion: CaptureConstants.D3D11SdkVersion,
            out var device,
            out _,
            out var context).ThrowOnFailure();

        try
        {
            var winrtDevice = CreateDirect3DDevice(device);
            var item = CreateItemForWindow(windowHandle);
            using var pool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                winrtDevice,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                CaptureConstants.FramePoolBufferCount,
                item.Size);
            using var session = pool.CreateCaptureSession(item);
            session.IsCursorCaptureEnabled = false;

            var frameCompletion = CreateFrameCompletion();
            pool.FrameArrived += OnFrameArrived;
            session.StartCapture();

            try
            {
                for (var frameNumber = 1;
                     frameNumber <= CaptureConstants.MaximumBlankFrameCount;
                     frameNumber++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using var frame = await frameCompletion.Task
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                    var result = CopyFrame(device, context, frame);
                    if (!IsBlank(result.Pixels) ||
                        frameNumber == CaptureConstants.MaximumBlankFrameCount)
                    {
                        return result;
                    }

                    await Task.Delay(
                        CaptureConstants.BlankFrameRetryDelayMilliseconds,
                        cancellationToken).ConfigureAwait(false);
                    frameCompletion = CreateFrameCompletion();
                }
            }
            finally
            {
                pool.FrameArrived -= OnFrameArrived;
            }

            throw new InvalidOperationException("WGC 프레임을 받지 못했습니다.");

            void OnFrameArrived(Direct3D11CaptureFramePool sender, object _)
            {
                Direct3D11CaptureFrame? frame = null;
                try
                {
                    frame = sender.TryGetNextFrame();
                    if (frame is not null && !frameCompletion.TrySetResult(frame))
                    {
                        frame.Dispose();
                    }
                }
                catch (Exception exception)
                {
                    frame?.Dispose();
                    frameCompletion.TrySetException(exception);
                }
            }
        }
        finally
        {
            (context as IDisposable)?.Dispose();
            (device as IDisposable)?.Dispose();
        }
    }

    public static bool IsBlank(byte[] pixels)
    {
        var chunks = MemoryMarshal.Cast<byte, long>(pixels.AsSpan());
        foreach (var chunk in chunks)
        {
            if (chunk != 0)
            {
                return false;
            }
        }

        for (var index = chunks.Length * sizeof(long); index < pixels.Length; index++)
        {
            if (pixels[index] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static TaskCompletionSource<Direct3D11CaptureFrame> CreateFrameCompletion()
    {
        return new TaskCompletionSource<Direct3D11CaptureFrame>(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static unsafe IDirect3DDevice CreateDirect3DDevice(D3D.ID3D11Device device)
    {
        var d3dDevicePointer = ComInterfaceMarshaller<D3D.ID3D11Device>.ConvertToUnmanaged(device);
        var dxgiDevicePointer = IntPtr.Zero;
        var graphicsDevicePointer = IntPtr.Zero;

        try
        {
            Marshal.QueryInterface(
                (IntPtr)d3dDevicePointer,
                in InteropIds.DxgiDevice,
                out dxgiDevicePointer).ThrowIfFailed("ID3D11Device.QueryInterface(IDXGIDevice)");
            CreateDirect3D11DeviceFromDXGIDevice(
                dxgiDevicePointer,
                out graphicsDevicePointer).ThrowIfFailed("CreateDirect3D11DeviceFromDXGIDevice");

            var managedDevice = MarshalInspectable<IDirect3DDevice>.FromAbi(graphicsDevicePointer);
            graphicsDevicePointer = IntPtr.Zero;
            return managedDevice;
        }
        finally
        {
            if (graphicsDevicePointer != IntPtr.Zero)
            {
                Marshal.Release(graphicsDevicePointer);
            }

            if (dxgiDevicePointer != IntPtr.Zero)
            {
                Marshal.Release(dxgiDevicePointer);
            }

            if (d3dDevicePointer is not null)
            {
                ComInterfaceMarshaller<D3D.ID3D11Device>.Free(d3dDevicePointer);
            }
        }
    }

    private static unsafe GraphicsCaptureItem CreateItemForWindow(HWND windowHandle)
    {
        using var factory = ActivationFactory.Get(InteropIds.GraphicsCaptureItemRuntimeClassName);
        var interopPointer = IntPtr.Zero;
        var itemPointer = IntPtr.Zero;

        try
        {
            Marshal.QueryInterface(
                factory.ThisPtr,
                in InteropIds.GraphicsCaptureItemInterop,
                out interopPointer).ThrowIfFailed(
                "GraphicsCaptureItem.QueryInterface(IGraphicsCaptureItemInterop)");

            var interop = ComInterfaceMarshaller<IGraphicsCaptureItemInterop>
                .ConvertToManaged((void*)interopPointer)!;
            interopPointer = IntPtr.Zero;
            interop.CreateForWindow(
                (IntPtr)windowHandle,
                in InteropIds.GraphicsCaptureItem,
                out itemPointer).ThrowIfFailed("GraphicsCaptureItem.CreateForWindow");

            var item = MarshalInspectable<GraphicsCaptureItem>.FromAbi(itemPointer);
            itemPointer = IntPtr.Zero;
            return item;
        }
        finally
        {
            if (itemPointer != IntPtr.Zero)
            {
                Marshal.Release(itemPointer);
            }

            if (interopPointer != IntPtr.Zero)
            {
                ComInterfaceMarshaller<IGraphicsCaptureItemInterop>.Free((void*)interopPointer);
            }
        }
    }

    private static unsafe CaptureFrame CopyFrame(
        D3D.ID3D11Device device,
        D3D.ID3D11DeviceContext context,
        Direct3D11CaptureFrame frame)
    {
        var capturedTexture = GetTexture(frame.Surface);
        try
        {
            var width = frame.ContentSize.Width;
            var height = frame.ContentSize.Height;
            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException("WGC가 빈 프레임을 반환했습니다.");
            }

            var description = new D3D.D3D11_TEXTURE2D_DESC
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DxgiCommon.DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                SampleDesc = new DxgiCommon.DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D.D3D11_USAGE.D3D11_USAGE_STAGING,
                BindFlags = 0,
                CPUAccessFlags = D3D.D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
                MiscFlags = 0,
            };

            device.CreateTexture2D(in description, pInitialData: null, out var stagingTexture);
            try
            {
                context.CopyResource(stagingTexture, capturedTexture);
                context.Map(
                    stagingTexture,
                    0,
                    D3D.D3D11_MAP.D3D11_MAP_READ,
                    0,
                    out var mapped);

                try
                {
                    var pixels = new byte[checked(width * height * CaptureConstants.BytesPerPixel)];
                    fixed (byte* destination = pixels)
                    {
                        var rowBytes = width * CaptureConstants.BytesPerPixel;
                        for (var row = 0; row < height; row++)
                        {
                            Buffer.MemoryCopy(
                                (byte*)mapped.pData + (row * mapped.RowPitch),
                                destination + (row * rowBytes),
                                rowBytes,
                                rowBytes);
                        }
                    }

                    return new CaptureFrame(pixels, width, height);
                }
                finally
                {
                    context.Unmap(stagingTexture, 0);
                }
            }
            finally
            {
                (stagingTexture as IDisposable)?.Dispose();
            }
        }
        finally
        {
            (capturedTexture as IDisposable)?.Dispose();
        }
    }

    private static unsafe D3D.ID3D11Texture2D GetTexture(IDirect3DSurface surface)
    {
        var surfacePointer = ((IWinRTObject)surface).NativeObject.ThisPtr;
        var accessPointer = IntPtr.Zero;
        var texturePointer = IntPtr.Zero;

        try
        {
            Marshal.QueryInterface(
                surfacePointer,
                in InteropIds.Direct3DDxgiInterfaceAccess,
                out accessPointer).ThrowIfFailed(
                "IDirect3DSurface.QueryInterface(IDirect3DDxgiInterfaceAccess)");

            var access = ComInterfaceMarshaller<IDirect3DDxgiInterfaceAccess>
                .ConvertToManaged((void*)accessPointer)!;
            accessPointer = IntPtr.Zero;
            access.GetInterface(
                in InteropIds.D3D11Texture2D,
                out texturePointer).ThrowIfFailed("IDirect3DDxgiInterfaceAccess.GetInterface");

            var texture = ComInterfaceMarshaller<D3D.ID3D11Texture2D>
                .ConvertToManaged((void*)texturePointer)!;
            texturePointer = IntPtr.Zero;
            return texture;
        }
        finally
        {
            if (texturePointer != IntPtr.Zero)
            {
                Marshal.Release(texturePointer);
            }

            if (accessPointer != IntPtr.Zero)
            {
                ComInterfaceMarshaller<IDirect3DDxgiInterfaceAccess>.Free((void*)accessPointer);
            }
        }
    }

    [LibraryImport("d3d11.dll")]
    private static partial int CreateDirect3D11DeviceFromDXGIDevice(
        IntPtr dxgiDevice,
        out IntPtr graphicsDevice);

    [GeneratedComInterface]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    internal partial interface IGraphicsCaptureItemInterop
    {
        // The generated COM ABI needs the native HWND value, not CsWin32's custom-marshalled wrapper.
        [PreserveSig]
        int CreateForWindow(IntPtr window, in Guid interfaceId, out IntPtr result);

        [PreserveSig]
        int CreateForMonitor(IntPtr monitor, in Guid interfaceId, out IntPtr result);
    }

    [GeneratedComInterface]
    [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    internal partial interface IDirect3DDxgiInterfaceAccess
    {
        [PreserveSig]
        int GetInterface(in Guid interfaceId, out IntPtr result);
    }

    private static void ThrowIfFailed(this int result, string operation)
    {
        if (result < 0)
        {
            throw new COMException($"{operation} failed with HRESULT 0x{result:X8}.", result);
        }
    }
}

internal sealed record CaptureFrame(byte[] Pixels, int Width, int Height);
