using System;
using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;

namespace TransApp;

public static class Direct3D11Helper
{
    [DllImport("d3d11.dll", EntryPoint = "D3D11CreateDevice", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern uint D3D11CreateDevice(IntPtr pAdapter, uint driverType, IntPtr software, uint flags, uint[] pFeatureLevels, uint featureLevels, uint sdkVersion, out IntPtr ppDevice, out uint pFeatureLevel, out IntPtr ppImmediateContext);

    [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern uint CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    public static IDirect3DDevice CreateDevice()
    {
        uint[] featureLevels = { 0xb000, 0xb100, 0xa000, 0xa100, 0x9100, 0x9200, 0x9300 };

        uint hr = D3D11CreateDevice(IntPtr.Zero, 1, IntPtr.Zero, 0x20, featureLevels, (uint)featureLevels.Length, 7, out IntPtr d3dPtr, out uint featureLevel, out IntPtr contextPtr);
        
        if (hr != 0) return null!;

        hr = CreateDirect3D11DeviceFromDXGIDevice(d3dPtr, out IntPtr graphicsDevicePtr);
        
        if (hr != 0) return null!;

        return MarshalInterface<IDirect3DDevice>.FromAbi(graphicsDevicePtr);
    }
}

[ComImport]
[Guid("AF865B01-902D-4C21-9570-4389084CDB07")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInspectable
{
    void GetIids(out uint iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out uint trustLevel);
}

public static class MarshalInterface<T>
{
    public static T FromAbi(IntPtr ptr) => WinRT.MarshalInspectable<T>.FromAbi(ptr);
}
