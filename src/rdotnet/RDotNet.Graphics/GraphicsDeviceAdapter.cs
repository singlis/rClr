﻿using RDotNet.Graphics.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace RDotNet.Graphics
{
    internal class GraphicsDeviceAdapter : IDisposable
    {
        private readonly IGraphicsDevice device;
        private readonly List<GCHandle> delegateHandles;
        private DeviceDescription description;
        private REngine engine;
        private IntPtr gdd;

        public GraphicsDeviceAdapter(IGraphicsDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException("device");
            }
            this.device = device;
            this.delegateHandles = new List<GCHandle>();
            this.gdd = IntPtr.Zero;
        }

        public REngine Engine
        {
            get { return this.engine; }
        }

        protected TDelegate GetFunction<TDelegate>() where TDelegate : class
        {
            return Engine.GetFunction<TDelegate>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.description.Dispose();
            if (disposing && this.gdd != IntPtr.Zero)
            {
                Kill();
            }
        }

        public void Kill()
        {
            this.GetFunction<GEkillDevice>()(this.description.DangerousGetHandle());
            this.gdd = IntPtr.Zero;
        }

        public void SetEngine(REngine engine)
        {
            if (gdd != IntPtr.Zero)
            {
                throw new InvalidOperationException("engine is already set");
            }
            if (engine == null)
            {
                throw new ArgumentNullException("engine");
            }
            if (this.engine != null)
            {
                throw new InvalidOperationException();
            }
            if (!engine.IsRunning)
            {
                throw new ArgumentException(null, "engine");
            }
            this.engine = engine;

            //this.GetFunction<R_GE_checkVersionOrDie>()(this.device.Version);
            this.GetFunction<R_CheckDeviceAvailable>()();
            var oldSuspended = GetInterruptsSuspended(engine);
            SetInterruptsSuspended(engine, true);

            this.description = new DeviceDescription();
            SetMethod();
            gdd = this.GetFunction<GEcreateDevDesc>()(this.description.DangerousGetHandle());
            this.GetFunction<GEaddDevice2>()(gdd, this.device.Name);

            SetInterruptsSuspended(engine, oldSuspended);
            if (GetInterruptsPending(engine) && !GetInterruptsSuspended(engine))
            {
                this.GetFunction<Rf_onintr>()();
            }
        }

        private static void SetInterruptsSuspended(REngine engine, bool value)
        {
            var pointer = engine.DangerousGetHandle("R_interrupts_suspended");
            Marshal.WriteInt32(pointer, Convert.ToInt32(value));
        }

        private static bool GetInterruptsSuspended(REngine engine)
        {
            var pointer = engine.DangerousGetHandle("R_interrupts_suspended");
            return Convert.ToBoolean(Marshal.ReadInt32(pointer));
        }

        private static bool GetInterruptsPending(REngine engine)
        {
            var pointer = engine.DangerousGetHandle("R_interrupts_pending");
            return Convert.ToBoolean(Marshal.ReadInt32(pointer));
        }

        private void SetMethod()
        {
            var activate = (_DevDesc_activate)Activate;
            Alloc(activate);
            this.description.SetMethod("activate", activate);
            var cap = (_DevDesc_cap)Capture;
            Alloc(cap);
            this.description.SetMethod("cap", cap);
            var circle = (_DevDesc_circle)DrawCircle;
            Alloc(circle);
            this.description.SetMethod("circle", circle);
            var clip = (_DevDesc_clip)Clip;
            Alloc(clip);
            this.description.SetMethod("clip", clip);
            var close = (_DevDesc_close)Close;
            Alloc(close);
            this.description.SetMethod("close", close);
            var deactivate = (_DevDesc_deactivate)Deactivate;
            Alloc(deactivate);
            this.description.SetMethod("deactivate", deactivate);
            var line = (_DevDesc_line)DrawLine;
            Alloc(line);
            this.description.SetMethod("line", line);
            var locator = (_DevDesc_locator)GetLocation;
            Alloc(locator);
            this.description.SetMethod("locator", locator);
            var metricInfo = (_DevDesc_metricInfo)GetMetricInfo;
            Alloc(metricInfo);
            this.description.SetMethod("metricInfo", metricInfo);
            var mode = (_DevDesc_mode)ChangeMode;
            Alloc(mode);
            this.description.SetMethod("mode", mode);
            var newPage = (_DevDesc_newPage)NewPage;
            Alloc(newPage);
            this.description.SetMethod("newPage", newPage);
            var path = (_DevDesc_path)DrawPath;
            Alloc(path);
            this.description.SetMethod("path", path);
            var polygon = (_DevDesc_polygon)DrawPolygon;
            Alloc(polygon);
            this.description.SetMethod("polygon", polygon);
            var polyline = (_DevDesc_Polyline)DrawPolyline;
            Alloc(polyline);
            this.description.SetMethod("polyline", polyline);
            var raster = (_DevDesc_raster)DrawRaster;
            Alloc(raster);
            this.description.SetMethod("raster", raster);
            var rect = (_DevDesc_rect)DrawRectangle;
            Alloc(rect);
            this.description.SetMethod("rect", rect);
            var size = (_DevDesc_size)Resize;
            Alloc(size);
            this.description.SetMethod("size", size);
            var strWidth = (_DevDesc_strWidth)MeasureWidth;
            Alloc(strWidth);
            this.description.SetMethod("strWidth", strWidth);
            var text = (_DevDesc_text)DrawText;
            Alloc(text);
            this.description.SetMethod("text", text);
            var strWidthUTF8 = (_DevDesc_strWidth)MeasureWidth;
            Alloc(strWidthUTF8);
            this.description.SetMethod("strWidthUTF8", strWidthUTF8);
            var textUTF8 = (_DevDesc_text)DrawText;
            Alloc(textUTF8);
            this.description.SetMethod("textUTF8", textUTF8);
            var newFrameConfirm = (_DevDesc_newFrameConfirm)ConfirmNewFrame;
            Alloc(newFrameConfirm);
            this.description.SetMethod("newFrameConfirm", newFrameConfirm);
            var getEvent = (_DevDesc_getEvent)GetEvent;
            Alloc(getEvent);
            this.description.SetMethod("getEvent", getEvent);
            var eventHelper = (_DevDesc_eventHelper)EventHelper;
            Alloc(eventHelper);
            this.description.SetMethod("eventHelper", eventHelper);
        }

        private void Alloc(Delegate d)
        {
            var handle = GCHandle.Alloc(d);
            this.delegateHandles.Add(handle);
        }

        private void FreeAll()
        {
            this.delegateHandles.ForEach(handle => handle.Free());
            this.delegateHandles.Clear();
        }

        private void Activate(IntPtr pDevDesc)
        {
            this.device.OnActivated(this.description);
        }

        private void Deactivate(IntPtr pDevDesc)
        {
            this.device.OnDeactivated(this.description);
        }

        private void NewPage(IntPtr gc, IntPtr dd)
        {
            var context = new GraphicsContext(gc);
            this.device.OnNewPageRequested(context, this.description);
        }

        private void Resize(out double left, out double right, out double bottom, out double top, IntPtr dd)
        {
            var rectangle = this.device.OnResized(this.description);
            left = rectangle.Left;
            right = rectangle.Right;
            bottom = rectangle.Bottom;
            top = rectangle.Top;
        }

        private void Close(IntPtr dd)
        {
            device.OnClosed(description);
            ClearDevDesc();
        }

        private void ClearDevDesc()
        {
            var geDevDesc = (GEDevDesc)Marshal.PtrToStructure(gdd, typeof(GEDevDesc));
            geDevDesc.dev = IntPtr.Zero;
            Marshal.StructureToPtr(geDevDesc, gdd, false);
        }

        private bool ConfirmNewFrame(IntPtr dd)
        {
            return this.device.ConfirmNewFrame(this.description);
        }

        private void ChangeMode(int mode, IntPtr dd)
        {
            if (mode == 0)
            {
                this.device.OnDrawStarted(this.description);
            }
            else if (mode == 1)
            {
                this.device.OnDrawStopped(this.description);
            }
        }

        private void DrawCircle(double x, double y, double r, IntPtr gc, IntPtr dd)
        {
            var context = new GraphicsContext(gc);
            var center = new Point(x, y);
            this.device.DrawCircle(center, r, context, this.description);
        }

        private void Clip(double x0, double x1, double y0, double y1, IntPtr dd)
        {
            var rectangle = new Rectangle(Math.Min(x0, x1), Math.Min(y0, y1), Math.Abs(x0 - x1), Math.Abs(y0 - y1));
            this.device.Clip(rectangle, this.description);
        }

        private bool GetLocation(out double x, out double y, IntPtr dd)
        {
            var location = this.device.GetLocation(this.description);
            if (!location.HasValue)
            {
                x = default(double);
                y = default(double);
                return false;
            }

            var p = location.Value;
            x = p.X;
            y = p.Y;
            return true;
        }

        private void DrawLine(double x1, double y1, double x2, double y2, IntPtr gc, IntPtr dd)
        {
            var context = new GraphicsContext(gc);
            var source = new Point(x1, y1);
            var destination = new Point(x2, y2);
            this.device.DrawLine(source, destination, context, this.description);
        }

        private void GetMetricInfo(int c, IntPtr gc, out double ascent, out double descent, out double width, IntPtr dd)
        {
            var context = new GraphicsContext(gc);
            var metric = this.device.GetMetricInfo(c, context, this.description);
            ascent = metric.Ascent;
            descent = metric.Descent;
            width = metric.Width;
        }

        private void DrawPolygon(int n, IntPtr x, IntPtr y, IntPtr gc, IntPtr dd)
        {
            var context = new GraphicsContext(gc);
            var points = GetPoints(n, x, y);
            this.device.DrawPolygon(points, context, this.description);
        }

        private void DrawPolyline(int n, IntPtr x, IntPtr y, IntPtr gc, IntPtr dd)
        {
            var context = new GraphicsContext(gc);
            var points = GetPoints(n, x, y);
            this.device.DrawPolyline(points, context, this.description);
        }

        private void DrawRectangle(double x0, double y0, double x1, double y1, IntPtr gc, IntPtr dd)
        {
            var context = new GraphicsContext(gc);
            var rectangle = new Rectangle(Math.Min(x0, x1), Math.Min(y0, y1), Math.Abs(x0 - x1), Math.Abs(y0 - y1));
            this.device.DrawRectangle(rectangle, context, this.description);
        }

        private void DrawPath(IntPtr x, IntPtr y, int npoly, IntPtr nper, bool winding, IntPtr gc, IntPtr dd)
        {
            var context = new GraphicsContext(gc);
            var points = GetPoints(x, y, npoly, nper);
            this.device.DrawPath(points, winding, context, this.description);
        }

        private void DrawRaster(IntPtr raster, int w, int h, double x, double y, double width, double height, double rot, bool interpolate, IntPtr gc, IntPtr dd)
        {
            var context = new GraphicsContext(gc);
            var output = new Raster(w, h);
            unchecked
            {
                for (var i = 0; i < w; i++)
                {
                    for (var j = 0; j < h; j++)
                    {
                        output[i, j] = Color.FromUInt32((uint)Marshal.ReadInt32(raster));
                        raster = IntPtr.Add(raster, sizeof(int));
                    }
                }
            }
            this.device.DrawRaster(output, new Rectangle(x, y, width, height), rot, interpolate, context, this.description);
        }

        private IntPtr Capture(IntPtr dd)
        {
            var raster = this.device.Capture(this.description);
            return Engine.CreateIntegerMatrix(raster).DangerousGetHandle();
        }

        private double MeasureWidth(string str, IntPtr gc, IntPtr dd)
        {
            var context = new GraphicsContext(gc);
            return this.device.MeasureWidth(str, context, this.description);
        }

        private void DrawText(double x, double y, string str, double rot, double hadj, IntPtr gc, IntPtr dd)
        {
            var context = new GraphicsContext(gc);
            this.device.DrawText(str, new Point(x, y), rot, hadj, context, this.description);
        }

        private IntPtr GetEvent(IntPtr sexp, string s)
        {
            return IntPtr.Zero;
        }

        private void EventHelper(IntPtr dd, int code)
        { }

        private IEnumerable<Point> GetPoints(int n, IntPtr x, IntPtr y)
        {
            return Enumerable.Range(0, n).Select(
               index => {
                   var offset = sizeof(double) * index;
                   var px = Utility.ReadDouble(x, offset);
                   var py = Utility.ReadDouble(y, offset);
                   return new Point(px, py);
               }
               );
        }

        private IEnumerable<IEnumerable<Point>> GetPoints(IntPtr x, IntPtr y, int npoly, IntPtr nper)
        {
            if (!Engine.IsRunning)
            {
                throw new InvalidOperationException();
            }
            for (var index = 0; index < npoly; index++)
            {
                var offset = sizeof(int) * index;
                var n = Marshal.ReadInt32(nper, offset);
                yield return GetPoints(n, x, y);
                var pointOffset = sizeof(double) * n;
                x = IntPtr.Add(x, pointOffset);
                y = IntPtr.Add(y, pointOffset);
            }
        }
    }
}