using System;
using System.Runtime.InteropServices;

namespace Visupra7.DirectShow
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsDual), Guid("56A868C0-0AD4-11CE-B03A-0020AF0BA770")]
    internal interface IMediaEventEx
    {
        [PreserveSig] int GetEventHandle(out IntPtr eventHandle);
        [PreserveSig] int GetEvent(out int eventCode, out IntPtr param1, out IntPtr param2, int timeoutMs);
        [PreserveSig] int WaitForCompletion(int timeoutMs, out int eventCode);
        [PreserveSig] int CancelDefaultHandling(int eventCode);
        [PreserveSig] int RestoreDefaultHandling(int eventCode);
        [PreserveSig] int FreeEventParams(int eventCode, IntPtr param1, IntPtr param2);
        [PreserveSig] int SetNotifyWindow(IntPtr hwnd, int msg, IntPtr instanceData);
        [PreserveSig] int SetNotifyFlags(int flags);
        [PreserveSig] int GetNotifyFlags(out int flags);
    }
}
