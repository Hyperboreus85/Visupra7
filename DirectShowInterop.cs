using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Visupra7.DirectShow
{
    internal static class Guids
    {
        public static readonly Guid VideoInputDeviceCategory = new Guid("860BB310-5D01-11D0-BD3B-00A0C911CE86");
        public static readonly Guid Capture = new Guid("FB6C4281-0353-11D1-905F-0000C0CC16BA");
        public static readonly Guid Preview = new Guid("FB6C4282-0353-11D1-905F-0000C0CC16BA");
        public static readonly Guid Video = new Guid("73646976-0000-0010-8000-00AA00389B71");
        public static readonly Guid RGB24 = new Guid("E436EB7D-524F-11CE-9F53-0020AF0BA770");
        public static readonly Guid VideoInfo = new Guid("05589F80-C356-11CE-BF01-00AA0055595A");
        public static readonly Guid IamStreamConfig = new Guid("C6E13340-30AC-11D0-A18C-00A0C9118956");
    }

    [ComImport, Guid("860BB310-5D01-11D0-BD3B-00A0C911CE86")]
    internal class VideoInputDeviceCategory { }
    [ComImport, Guid("E436EBB3-524F-11CE-9F53-0020AF0BA770")]
    internal class FilterGraph { }
    [ComImport, Guid("BF87B6E1-8C27-11D0-B3F0-00AA003761C5")]
    internal class CaptureGraphBuilder2 { }
    [ComImport, Guid("C1F400A0-3F08-11D3-9F0B-006008039E37")]
    internal class SampleGrabber { }
    [ComImport, Guid("62BE5D10-60EB-11D0-BD3B-00A0C911CE86")]
    internal class SystemDeviceEnum { }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("29840822-5B84-11D0-BD3B-00A0C911CE86")]
    internal interface ICreateDevEnum
    {
        [PreserveSig] int CreateClassEnumerator([In] ref Guid category, [Out] out IEnumMoniker enumMoniker, int flags);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("55272A00-42CB-11CE-8135-00AA004BB851")]
    internal interface IPropertyBag
    {
        [PreserveSig] int Read([MarshalAs(UnmanagedType.LPWStr)] string name, [Out, MarshalAs(UnmanagedType.Struct)] out object value, IntPtr errorLog);
        [PreserveSig] int Write([MarshalAs(UnmanagedType.LPWStr)] string name, [In, MarshalAs(UnmanagedType.Struct)] ref object value);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("56A86895-0AD4-11CE-B03A-0020AF0BA770")]
    internal interface IBaseFilter
    {
        [PreserveSig] int GetClassID(out Guid clsid);
        [PreserveSig] int Stop(); [PreserveSig] int Pause(); [PreserveSig] int Run(long start);
        [PreserveSig] int GetState(int timeout, out int state);
        [PreserveSig] int SetSyncSource(IntPtr clock); [PreserveSig] int GetSyncSource(out IntPtr clock);
        [PreserveSig] int EnumPins(out IntPtr enumPins); [PreserveSig] int FindPin([MarshalAs(UnmanagedType.LPWStr)] string id, out IntPtr pin);
        [PreserveSig] int QueryFilterInfo(out FilterInfo info); [PreserveSig] int JoinFilterGraph(IntPtr graph, [MarshalAs(UnmanagedType.LPWStr)] string name);
        [PreserveSig] int QueryVendorInfo([MarshalAs(UnmanagedType.LPWStr)] out string vendor);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct FilterInfo { [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Name; public IntPtr Graph; }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("56A868A9-0AD4-11CE-B03A-0020AF0BA770")]
    internal interface IGraphBuilder
    {
        [PreserveSig] int AddFilter([In] IBaseFilter filter, [MarshalAs(UnmanagedType.LPWStr)] string name);
        [PreserveSig] int RemoveFilter([In] IBaseFilter filter); [PreserveSig] int EnumFilters(out IntPtr enumFilters);
        [PreserveSig] int FindFilterByName([MarshalAs(UnmanagedType.LPWStr)] string name, out IBaseFilter filter);
        [PreserveSig] int ConnectDirect(IntPtr outputPin, IntPtr inputPin, [In] AMMediaType mediaType);
        [PreserveSig] int Reconnect(IntPtr pin); [PreserveSig] int Disconnect(IntPtr pin); [PreserveSig] int SetDefaultSyncSource();
        [PreserveSig] int Connect(IntPtr outputPin, IntPtr inputPin); [PreserveSig] int Render(IntPtr outputPin);
        [PreserveSig] int RenderFile([MarshalAs(UnmanagedType.LPWStr)] string file, [MarshalAs(UnmanagedType.LPWStr)] string playlist);
        [PreserveSig] int AddSourceFilter([MarshalAs(UnmanagedType.LPWStr)] string file, [MarshalAs(UnmanagedType.LPWStr)] string name, out IBaseFilter filter);
        [PreserveSig] int SetLogFile(IntPtr file); [PreserveSig] int Abort(); [PreserveSig] int ShouldOperationContinue();
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("93E5A4E0-2D50-11D2-ABFA-00A0C9C6E38D")]
    internal interface ICaptureGraphBuilder2
    {
        [PreserveSig] int SetFiltergraph([In] IGraphBuilder graph); [PreserveSig] int GetFiltergraph(out IGraphBuilder graph);
        [PreserveSig] int SetOutputFileName([In] ref Guid type, [MarshalAs(UnmanagedType.LPWStr)] string file, out IBaseFilter mux, out IntPtr sink);
        [PreserveSig] int FindInterface([In] ref Guid category, [In] ref Guid type, [In, MarshalAs(UnmanagedType.IUnknown)] object source, [In] ref Guid iid, [Out, MarshalAs(UnmanagedType.IUnknown)] out object result);
        [PreserveSig] int RenderStream([In] ref Guid category, [In] ref Guid type, [In, MarshalAs(UnmanagedType.IUnknown)] object source, [In] IBaseFilter compressor, [In] IBaseFilter renderer);
        [PreserveSig] int ControlStream([In] ref Guid category, [In] ref Guid type, [In, MarshalAs(UnmanagedType.IUnknown)] object filter, long start, long stop, short startCookie, short stopCookie);
        [PreserveSig] int AllocCapFile([MarshalAs(UnmanagedType.LPWStr)] string file, long size);
        [PreserveSig] int CopyCaptureFile([MarshalAs(UnmanagedType.LPWStr)] string oldFile, [MarshalAs(UnmanagedType.LPWStr)] string newFile, int allowEscAbort, IntPtr callback);
        [PreserveSig] int FindPin([MarshalAs(UnmanagedType.IUnknown)] object source, int direction, [In] ref Guid category, [In] ref Guid type, [MarshalAs(UnmanagedType.Bool)] bool unconnected, int index, out IntPtr pin);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("56A868B1-0AD4-11CE-B03A-0020AF0BA770")]
    internal interface IMediaControl
    {
        [PreserveSig] int Run(); [PreserveSig] int Pause(); [PreserveSig] int Stop();
        [PreserveSig] int GetState(int timeout, out int state); [PreserveSig] int RenderFile([MarshalAs(UnmanagedType.BStr)] string file);
        [PreserveSig] int AddSourceFilter([MarshalAs(UnmanagedType.BStr)] string file, out object filterInfo);
        [PreserveSig] int get_FilterCollection(out object filters); [PreserveSig] int get_RegFilterCollection(out object filters);
        [PreserveSig] int StopWhenReady();
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsDual), Guid("56A868B4-0AD4-11CE-B03A-0020AF0BA770")]
    internal interface IVideoWindow
    {
        [PreserveSig] int put_Caption([MarshalAs(UnmanagedType.BStr)] string caption); [PreserveSig] int get_Caption([MarshalAs(UnmanagedType.BStr)] out string caption);
        [PreserveSig] int put_WindowStyle(int style); [PreserveSig] int get_WindowStyle(out int style);
        [PreserveSig] int put_WindowStyleEx(int style); [PreserveSig] int get_WindowStyleEx(out int style);
        [PreserveSig] int put_AutoShow(int autoShow); [PreserveSig] int get_AutoShow(out int autoShow);
        [PreserveSig] int put_WindowState(int state); [PreserveSig] int get_WindowState(out int state);
        [PreserveSig] int put_BackgroundPalette(int backgroundPalette); [PreserveSig] int get_BackgroundPalette(out int backgroundPalette);
        [PreserveSig] int put_Visible(int visible); [PreserveSig] int get_Visible(out int visible);
        [PreserveSig] int put_Left(int left); [PreserveSig] int get_Left(out int left); [PreserveSig] int put_Width(int width); [PreserveSig] int get_Width(out int width);
        [PreserveSig] int put_Top(int top); [PreserveSig] int get_Top(out int top); [PreserveSig] int put_Height(int height); [PreserveSig] int get_Height(out int height);
        [PreserveSig] int put_Owner(IntPtr owner); [PreserveSig] int get_Owner(out IntPtr owner); [PreserveSig] int put_MessageDrain(IntPtr drain); [PreserveSig] int get_MessageDrain(out IntPtr drain);
        [PreserveSig] int get_BorderColor(out int color); [PreserveSig] int put_BorderColor(int color); [PreserveSig] int get_FullScreenMode(out int fullScreen); [PreserveSig] int put_FullScreenMode(int fullScreen);
        [PreserveSig] int SetWindowForeground(int focus); [PreserveSig] int NotifyOwnerMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);
        [PreserveSig] int SetWindowPosition(int left, int top, int width, int height); [PreserveSig] int GetWindowPosition(out int left, out int top, out int width, out int height);
        [PreserveSig] int GetMinIdealImageSize(out int width, out int height); [PreserveSig] int GetMaxIdealImageSize(out int width, out int height);
        [PreserveSig] int GetRestorePosition(out int left, out int top, out int width, out int height); [PreserveSig] int HideCursor(int hide); [PreserveSig] int IsCursorHidden(out int hidden);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("6B652FFF-11FE-4FCE-92AD-0266B5D7C78F")]
    internal interface ISampleGrabber
    {
        [PreserveSig] int SetOneShot([MarshalAs(UnmanagedType.Bool)] bool oneShot); [PreserveSig] int SetMediaType([In] AMMediaType mediaType);
        [PreserveSig] int GetConnectedMediaType([In, Out] AMMediaType mediaType); [PreserveSig] int SetBufferSamples([MarshalAs(UnmanagedType.Bool)] bool bufferThem);
        [PreserveSig] int GetCurrentBuffer(ref int bufferSize, IntPtr buffer); [PreserveSig] int GetCurrentSample(IntPtr sample);
        [PreserveSig] int SetCallback(ISampleGrabberCB callback, int whichMethodToCallback);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0579154A-2B53-4994-B0D0-E773148EFF85")]
    internal interface ISampleGrabberCB
    {
        [PreserveSig] int SampleCB(double sampleTime, IntPtr sample); [PreserveSig] int BufferCB(double sampleTime, IntPtr buffer, int bufferLen);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("C6E13340-30AC-11D0-A18C-00A0C9118956")]
    internal interface IAMStreamConfig
    {
        [PreserveSig] int SetFormat([In] AMMediaType mediaType); [PreserveSig] int GetFormat([Out] out AMMediaType mediaType);
        [PreserveSig] int GetNumberOfCapabilities(out int count, out int size); [PreserveSig] int GetStreamCaps(int index, [Out] out AMMediaType mediaType, IntPtr caps);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class AMMediaType
    {
        public Guid majorType; public Guid subType; [MarshalAs(UnmanagedType.Bool)] public bool fixedSizeSamples;
        [MarshalAs(UnmanagedType.Bool)] public bool temporalCompression; public int sampleSize; public Guid formatType;
        public IntPtr unkPtr; public int formatSize; public IntPtr formatPtr;
    }

    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] internal struct BitmapInfoHeader { public int Size, Width, Height; public short Planes, BitCount; public int Compression, ImageSize, XPelsPerMeter, YPelsPerMeter, ClrUsed, ClrImportant; }
    [StructLayout(LayoutKind.Sequential)] internal struct VideoInfoHeader { public Rect SrcRect, TargetRect; public int BitRate, BitErrorRate; public long AvgTimePerFrame; public BitmapInfoHeader BmiHeader; }

    internal static class DsUtil
    {
        public static void Check(int hr, string operation) { if (hr < 0) Marshal.ThrowExceptionForHR(hr, new IntPtr(-1)); }
        public static void FreeMediaType(AMMediaType mt)
        {
            if (mt == null) return;
            if (mt.formatPtr != IntPtr.Zero) { Marshal.FreeCoTaskMem(mt.formatPtr); mt.formatPtr = IntPtr.Zero; }
            if (mt.unkPtr != IntPtr.Zero) { Marshal.Release(mt.unkPtr); mt.unkPtr = IntPtr.Zero; }
        }
        public static void Release(object value) { if (value != null && Marshal.IsComObject(value)) try { Marshal.FinalReleaseComObject(value); } catch { } }
    }
}
