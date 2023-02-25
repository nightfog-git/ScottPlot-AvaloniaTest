using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Data;
using ScottPlot;
using ScottPlot.Control;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Platform;
using Avalonia.Skia;
using System.Globalization;

namespace AvaloniaApplication3
{
    public class CustomPlot : Control, IPlotControl
    {
        //public static readonly DirectProperty<CustomPlot, Plot> PlotProperty;
        public static readonly DirectProperty<CustomPlot, object> InvalidateCommandProperty;

        //private Plot _plot;

        //public Plot Plot
        //{
        //    get => _plot;
        //    set => SetAndRaise(PlotProperty, ref _plot, value);
        //}

        public Plot Plot { get; } = new Plot();

        public object InvalidateCommand
        {
            get => null;
            set => Refresh();
        }

        public Interaction Interaction { get; private set; }

        static CustomPlot()
        {
            //PlotProperty = AvaloniaProperty.RegisterDirect<CustomPlot, Plot>(nameof(Plot),
            //    o => o.Plot, (o, v) => o.Plot = v, defaultBindingMode: BindingMode.OneTime);

            InvalidateCommandProperty = AvaloniaProperty.RegisterDirect<CustomPlot, object>(nameof(InvalidateCommand),
                o => o.InvalidateCommand, (o, v) => o.InvalidateCommand = v);

            ClipToBoundsProperty.OverrideDefaultValue<CustomPlot>(true);
            FocusableProperty.OverrideDefaultValue<CustomPlot>(true);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            Interaction = new Interaction(this);
            base.OnAttachedToVisualTree(e);
        }

        public void Refresh()
        {
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            // check for designer to work
            //if (Plot == null) return;

            using (var drawOperation = new DrawOperation(Plot, new Rect(Bounds.Size)))
            {
                context.Custom(drawOperation);
            }

            // test invalidate loop
            //Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }

        public void Replace(Interaction interaction)
        {
            Interaction = interaction;
        }

        public void ShowContextMenu(Pixel position)
        {
            // no-op for now
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            Interaction.MouseDown(
                position: e.ToPixel(this),
                button: e.GetCurrentPoint(this).Properties.PointerUpdateKind.ToButton());

            e.Pointer.Capture(this);

            if (e.ClickCount == 2) Interaction.DoubleClick();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            Interaction.MouseUp(
                position: e.ToPixel(this),
                button: e.GetCurrentPoint(this).Properties.PointerUpdateKind.ToButton());

            e.Pointer.Capture(null);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            Interaction.OnMouseMove(e.ToPixel(this));
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            // Flipping Delta.X and Delta.Y is not required anymore, when Shift is pressed
            float delta = (float)e.Delta.Y;
            if (delta != 0)
            {
                Interaction.MouseWheelVertical(e.ToPixel(this), delta);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            Interaction.KeyDown(e.ToKey());
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            Interaction.KeyUp(e.ToKey());
        }

        private class DrawOperation : ICustomDrawOperation
        {
            private static readonly FormattedText _noSkia;
            private readonly Plot _plot;

            public Rect Bounds { get; }

            static DrawOperation()
            {
                _noSkia = new FormattedText("Current rendering API is not Skia", CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.Red);
            }

            // Disposing of plot is not our responsibility
            public DrawOperation(Plot plot, Rect bounds)
            {
                _plot = plot ?? throw new ArgumentNullException();
                Bounds = bounds;
            }

            public void Render(IDrawingContextImpl context)
            {
                ISkiaSharpApiLeaseFeature leaseFeature = context.GetFeature<ISkiaSharpApiLeaseFeature>();

                if (leaseFeature == null)
                {
                    RenderFallback(context);
                    return;
                }

                using (ISkiaSharpApiLease lease = leaseFeature.Lease())
                {
                    _plot.Render(lease.SkSurface);
                }

                //// todo
                //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                //sw.Start();
                //_plot.Render(lease.SkSurface);
                //sw.Stop();
                //Console.WriteLine(sw.Elapsed.TotalMilliseconds.ToString("F2"));
                //System.Diagnostics.Trace.WriteLine(sw.Elapsed.TotalMilliseconds.ToString("F2"));
            }

            private void RenderFallback(IDrawingContextImpl context)
            {
                using (var c = new DrawingContext(context, false))
                {
                    c.DrawText(_noSkia, new Point());
                }
            }

            public bool HitTest(Point p) => Bounds.ContainsExclusive(p);

            public bool Equals(ICustomDrawOperation otherInstance) => (this == otherInstance);

            public void Dispose() { }
        } // DrawOperation class

    }
}
