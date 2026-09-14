using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Visupra7.DirectShow;

namespace Visupra7
{
    internal sealed class WebcamDevice : IDisposable
    {
        public string Name { get; private set; }
        public string MonikerName { get; private set; }
        internal IMoniker Moniker { get; private set; }
        public WebcamDevice(string name, string monikerName, IMoniker moniker) { Name = name; MonikerName = monikerName; Moniker = moniker; }
        public override string ToString() { return Name; }
        public void Dispose() { DsUtil.Release(Moniker); Moniker = null; }
    }

    internal sealed class VideoFormat
    {
        public int CapabilityIndex, Width, Height, Fps;
        public override string ToString() { return Width + " x " + Height + (Fps > 0 ? " @ " + Fps + " fps" : ""); }
    }

    internal sealed class FrameData
    {
        public readonly byte[] Buffer; public readonly int Width, Height, Stride; public readonly bool BottomUp; public readonly DateTime CapturedAt;
        public FrameData(byte[] buffer, int width, int height, int stride, bool bottomUp) { Buffer = buffer; Width = width; Height = height; Stride = stride; BottomUp = bottomUp; CapturedAt = DateTime.Now; }
    }

    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    internal sealed class WebcamCapture : ISampleGrabberCB, IDisposable
    {
        private readonly Logger log;
        private readonly object frameSync = new object();
        private IGraphBuilder graph; private ICaptureGraphBuilder2 builder; private IBaseFilter source; private IBaseFilter grabberFilter;
        private ISampleGrabber grabber; private IMediaControl mediaControl; private IVideoWindow videoWindow; private IMediaEventEx mediaEvent;
        private FrameData latest; private volatile bool acceptingFrames; private int width, height, stride; private bool bottomUp;
        public event Action<FrameData> FrameArrived;
        public bool IsRunning { get { return graph != null; } }
        public int Width { get { return width; } } public int Height { get { return height; } }
        public int Fps { get; private set; }

        public WebcamCapture(Logger logger) { log = logger; }

        public static List<WebcamDevice> Enumerate(Logger log)
        {
            var devices = new List<WebcamDevice>(); ICreateDevEnum devEnum = null; IEnumMoniker enumMoniker = null;
            try
            {
                devEnum = (ICreateDevEnum)new SystemDeviceEnum(); Guid category = Guids.VideoInputDeviceCategory;
                int hr = devEnum.CreateClassEnumerator(ref category, out enumMoniker, 0);
                if (hr != 0 || enumMoniker == null) { log.Info("Nessuna webcam rilevata"); return devices; }
                var values = new IMoniker[1]; IntPtr fetched = Marshal.AllocCoTaskMem(4);
                try
                {
                    while (enumMoniker.Next(1, values, fetched) == 0)
                    {
                        IMoniker moniker = values[0]; string name = "Webcam"; string display = ""; object bagObject = null;
                        try
                        {
                            Guid bagId = typeof(IPropertyBag).GUID; moniker.BindToStorage(null, null, ref bagId, out bagObject);
                            object value; if (((IPropertyBag)bagObject).Read("FriendlyName", out value, IntPtr.Zero) == 0) name = Convert.ToString(value);
                            moniker.GetDisplayName(null, null, out display);
                            devices.Add(new WebcamDevice(name, display, moniker)); values[0] = null;
                            log.Info("Webcam rilevata: " + name + "; moniker: " + display);
                        }
                        catch (Exception ex) { log.Error("Errore nella lettura di una webcam", ex); DsUtil.Release(moniker); }
                        finally { DsUtil.Release(bagObject); }
                    }
                }
                finally { Marshal.FreeCoTaskMem(fetched); }
            }
            catch (Exception ex) { log.Error("Enumerazione webcam fallita", ex); }
            finally { DsUtil.Release(enumMoniker); DsUtil.Release(devEnum); }
            return devices;
        }

        public static List<VideoFormat> GetFormats(WebcamDevice device, Logger log)
        {
            var result = new List<VideoFormat>(); IGraphBuilder tempGraph = null; ICaptureGraphBuilder2 tempBuilder = null; IBaseFilter tempSource = null; object configObject = null;
            try
            {
                tempGraph = (IGraphBuilder)new FilterGraph(); tempBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2(); DsUtil.Check(tempBuilder.SetFiltergraph(tempGraph), "SetFiltergraph");
                tempSource = Bind(device); DsUtil.Check(tempGraph.AddFilter(tempSource, device.Name), "AddFilter");
                Guid category = Guids.Capture, type = Guids.Video, iid = Guids.IamStreamConfig;
                DsUtil.Check(tempBuilder.FindInterface(ref category, ref type, tempSource, ref iid, out configObject), "FindInterface IAMStreamConfig");
                IAMStreamConfig config = (IAMStreamConfig)configObject; int count, capSize; DsUtil.Check(config.GetNumberOfCapabilities(out count, out capSize), "GetNumberOfCapabilities");
                IntPtr caps = Marshal.AllocCoTaskMem(Math.Max(capSize, 1));
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        var mt = new AMMediaType();
                        try
                        {
                            if (config.GetStreamCaps(i, mt, caps) == 0 && mt.formatType == Guids.VideoInfo && mt.formatPtr != IntPtr.Zero)
                            {
                                var vih = (VideoInfoHeader)Marshal.PtrToStructure(mt.formatPtr, typeof(VideoInfoHeader)); int fps = vih.AvgTimePerFrame > 0 ? (int)Math.Round(10000000.0 / vih.AvgTimePerFrame) : 0;
                                int h = Math.Abs(vih.BmiHeader.Height); bool duplicate = result.Exists(delegate(VideoFormat f) { return f.Width == vih.BmiHeader.Width && f.Height == h && f.Fps == fps; });
                                if (!duplicate) result.Add(new VideoFormat { CapabilityIndex = i, Width = vih.BmiHeader.Width, Height = h, Fps = fps });
                            }
                        }
                        finally { DsUtil.FreeMediaType(mt); }
                    }
                }
                finally { Marshal.FreeCoTaskMem(caps); }
            }
            catch (Exception ex) { log.Error("Lettura formati webcam fallita", ex); }
            finally { DsUtil.Release(configObject); if (tempGraph != null && tempSource != null) tempGraph.RemoveFilter(tempSource); DsUtil.Release(tempSource); DsUtil.Release(tempBuilder); DsUtil.Release(tempGraph); }
            return result;
        }

        public void Start(WebcamDevice device, VideoFormat format, IntPtr previewHandle, int previewWidth, int previewHeight)
        {
            if (IsRunning) Stop();
            try
            {
                graph = (IGraphBuilder)new FilterGraph(); builder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2(); DsUtil.Check(builder.SetFiltergraph(graph), "SetFiltergraph");
                source = Bind(device); DsUtil.Check(graph.AddFilter(source, device.Name), "AddFilter sorgente"); if (format != null) ApplyFormat(format);
                grabber = (ISampleGrabber)new SampleGrabber(); grabberFilter = (IBaseFilter)grabber;
                var requested = new AMMediaType { majorType = Guids.Video, subType = Guids.RGB24, formatType = Guids.VideoInfo };
                DsUtil.Check(grabber.SetMediaType(requested), "SetMediaType RGB24"); DsUtil.Check(grabber.SetBufferSamples(false), "SetBufferSamples"); DsUtil.Check(grabber.SetOneShot(false), "SetOneShot");
                DsUtil.Check(graph.AddFilter(grabberFilter, "Frame Grabber"), "AddFilter grabber");
                Guid preview = Guids.Preview, video = Guids.Video; int hr = builder.RenderStream(ref preview, ref video, source, grabberFilter, null);
                if (hr < 0) { log.Warn("Pin Preview non disponibile; uso il pin Capture"); Guid capture = Guids.Capture; DsUtil.Check(builder.RenderStream(ref capture, ref video, source, grabberFilter, null), "RenderStream Capture"); }
                ReadConnectedFormat(); DsUtil.Check(grabber.SetCallback(this, 1), "SetCallback"); mediaControl = (IMediaControl)graph; videoWindow = graph as IVideoWindow; mediaEvent = graph as IMediaEventEx;
                if (videoWindow == null) throw new InvalidOperationException("Il renderer DirectShow non espone IVideoWindow.");
                const int WS_CHILD = 0x40000000, WS_CLIPSIBLINGS = 0x04000000, WS_CLIPCHILDREN = 0x02000000;
                DsUtil.Check(videoWindow.put_Owner(previewHandle), "Owner anteprima"); DsUtil.Check(videoWindow.put_WindowStyle(WS_CHILD | WS_CLIPSIBLINGS | WS_CLIPCHILDREN), "Stile anteprima");
                videoWindow.SetWindowPosition(0, 0, Math.Max(1, previewWidth), Math.Max(1, previewHeight)); videoWindow.put_Visible(-1); acceptingFrames = true; DsUtil.Check(mediaControl.Run(), "Avvio graph");
                log.Info("Webcam avviata: " + device.Name + "; formato: " + width + "x" + height + " @ " + Fps + " fps; RGB24 stride " + stride);
            }
            catch { Stop(); throw; }
        }

        private void ApplyFormat(VideoFormat selected)
        {
            object configObject = null; Guid category = Guids.Capture, type = Guids.Video, iid = Guids.IamStreamConfig;
            try
            {
                DsUtil.Check(builder.FindInterface(ref category, ref type, source, ref iid, out configObject), "FindInterface IAMStreamConfig");
                IAMStreamConfig config = (IAMStreamConfig)configObject; int count, capSize; DsUtil.Check(config.GetNumberOfCapabilities(out count, out capSize), "GetNumberOfCapabilities");
                IntPtr caps = Marshal.AllocCoTaskMem(Math.Max(capSize, 1)); var mt = new AMMediaType();
                try { DsUtil.Check(config.GetStreamCaps(selected.CapabilityIndex, mt, caps), "GetStreamCaps"); DsUtil.Check(config.SetFormat(mt), "SetFormat"); }
                finally { DsUtil.FreeMediaType(mt); Marshal.FreeCoTaskMem(caps); }
            }
            finally { DsUtil.Release(configObject); }
        }

        private void ReadConnectedFormat()
        {
            var mt = new AMMediaType();
            try
            {
                DsUtil.Check(grabber.GetConnectedMediaType(mt), "GetConnectedMediaType"); if (mt.formatPtr == IntPtr.Zero || mt.formatType != Guids.VideoInfo) throw new NotSupportedException("Formato DirectShow non VIDEOINFOHEADER.");
                var vih = (VideoInfoHeader)Marshal.PtrToStructure(mt.formatPtr, typeof(VideoInfoHeader)); width = vih.BmiHeader.Width; height = Math.Abs(vih.BmiHeader.Height); bottomUp = vih.BmiHeader.Height > 0;
                stride = vih.BmiHeader.ImageSize > 0 ? vih.BmiHeader.ImageSize / height : ((width * 3 + 3) & ~3); Fps = vih.AvgTimePerFrame > 0 ? (int)Math.Round(10000000.0 / vih.AvgTimePerFrame) : 30;
            }
            finally { DsUtil.FreeMediaType(mt); }
        }

        public FrameData GetLatestFrame() { lock (frameSync) { return latest == null ? null : new FrameData((byte[])latest.Buffer.Clone(), latest.Width, latest.Height, latest.Stride, latest.BottomUp); } }
        public void AttachPreview(IntPtr ownerHandle, int w, int h)
        {
            if (videoWindow == null) throw new InvalidOperationException("Anteprima DirectShow non attiva.");
            const int WS_CHILD = 0x40000000, WS_CLIPSIBLINGS = 0x04000000, WS_CLIPCHILDREN = 0x02000000;
            DsUtil.Check(videoWindow.put_Visible(0), "Nascondi anteprima");
            DsUtil.Check(videoWindow.put_Owner(ownerHandle), "Nuovo owner anteprima");
            DsUtil.Check(videoWindow.put_WindowStyle(WS_CHILD | WS_CLIPSIBLINGS | WS_CLIPCHILDREN), "Stile anteprima");
            DsUtil.Check(videoWindow.SetWindowPosition(0, 0, Math.Max(1, w), Math.Max(1, h)), "Dimensione anteprima");
            DsUtil.Check(videoWindow.put_Visible(-1), "Mostra anteprima");
        }

        public void ResizePreview(int w, int h) { if (videoWindow != null) try { videoWindow.SetWindowPosition(0, 0, Math.Max(1, w), Math.Max(1, h)); } catch { } }
        public bool PollDeviceLost()
        {
            if (mediaEvent == null) return false; int code; IntPtr p1, p2; bool lost = false;
            while (mediaEvent.GetEvent(out code, out p1, out p2, 0) == 0) { try { if (code == 0x001F || code == 0x0002) lost = true; } finally { mediaEvent.FreeEventParams(code, p1, p2); } } return lost;
        }

        public void Stop()
        {
            bool wasRunning = graph != null; acceptingFrames = false; try { if (grabber != null) grabber.SetCallback(null, 0); } catch { }
            try { if (mediaControl != null) mediaControl.Stop(); } catch (Exception ex) { log.Error("Errore arresto graph", ex); }
            try { if (videoWindow != null) { videoWindow.put_Visible(0); videoWindow.put_Owner(IntPtr.Zero); } } catch { }
            lock (frameSync) { latest = null; }
            DsUtil.Release(mediaEvent); DsUtil.Release(videoWindow); DsUtil.Release(mediaControl); DsUtil.Release(grabber); DsUtil.Release(grabberFilter); DsUtil.Release(source); DsUtil.Release(builder); DsUtil.Release(graph);
            mediaEvent = null; videoWindow = null; mediaControl = null; grabber = null; grabberFilter = null; source = null; builder = null; graph = null; width = height = stride = 0; Fps = 0;
            if (wasRunning) log.Info("Webcam fermata");
        }

        int ISampleGrabberCB.SampleCB(double sampleTime, IntPtr sample) { return 0; }
        int ISampleGrabberCB.BufferCB(double sampleTime, IntPtr buffer, int bufferLen)
        {
            if (!acceptingFrames || buffer == IntPtr.Zero || bufferLen <= 0) return 0;
            try { lock (frameSync) { if (latest == null || latest.Buffer.Length != bufferLen) latest = new FrameData(new byte[bufferLen], width, height, stride, bottomUp); Marshal.Copy(buffer, latest.Buffer, 0, bufferLen); var handler = FrameArrived; if (handler != null) handler(latest); } } catch { }
            return 0;
        }
        private static IBaseFilter Bind(WebcamDevice device) { object value; Guid iid = typeof(IBaseFilter).GUID; device.Moniker.BindToObject(null, null, ref iid, out value); return (IBaseFilter)value; }
        public void Dispose() { Stop(); }
    }
}
