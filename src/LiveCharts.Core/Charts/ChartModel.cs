﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using LiveCharts.Core.Abstractions;
using LiveCharts.Core.Coordinates;
using LiveCharts.Core.Data;
using LiveCharts.Core.DataSeries;
using LiveCharts.Core.Dimensions;
using LiveCharts.Core.Drawing;
using LiveCharts.Core.Events;
using Size = LiveCharts.Core.Drawing.Size;

namespace LiveCharts.Core.Charts
{
    /// <summary>
    /// Defines a chart.
    /// </summary>
    public abstract class ChartModel : IDisposable
    {
        private static int _colorCount;
        private Task _delayer;
        private IList<Color> _colors;
        private readonly HashSet<IResource> _resources = new HashSet<IResource>();
        private readonly Dictionary<string, object> _resourcesCollections = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ChartModel"/> class.
        /// </summary>
        /// <param name="view">The chart view.</param>
        protected ChartModel(IChartView view)
        {
            View = view;
            view.ChartViewLoaded += sender =>
            {
                IsViewInitialized = true;
                Invalidate(sender);
            };
            view.ChartViewResized += Invalidate;
            view.PointerMoved += ViewOnPointerMoved;
            view.PropertyChanged += InvalidatePropertyChanged;
            TooltipTimoutTimer = new Timer();
            TooltipTimoutTimer.Elapsed += (sender, args) =>
            {
                View.InvokeOnUiThread(() => { ToolTip.Hide(View); });
                TooltipTimoutTimer.Stop();
            };
        }

#if DEBUG
        /// <summary>
        /// [DEBUG PROPERTY] Invalidation debug count.
        /// </summary>
        public int InvalidateCount { get; set; }
#endif

        /// <summary>
        /// Gets the update identifier.
        /// </summary>
        /// <value>
        /// The update identifier.
        /// </value>
        public object UpdateId { get; private set; } = new object();

        /// <summary>
        /// Gets or sets the draw area location.
        /// </summary>
        /// <value>
        /// The draw area location.
        /// </value>
        public Point DrawAreaLocation { get; set; }

        /// <summary>
        /// Gets or sets the size of the draw area.
        /// </summary>
        /// <value>
        /// The size of the draw area.
        /// </value>
        public Size DrawAreaSize { get; set; }

        /// <summary>
        /// Gets the data range matrix.
        /// </summary>
        /// <value>
        /// The data range matrix.
        /// </value>
        public DimensionRange[][] DataRangeMatrix { get; protected set; }

        /// <summary>
        /// Gets the chart view.
        /// </summary>
        /// <value>
        /// The chart view.
        /// </value>
        public IChartView View { get; }

        /// <summary>
        /// Gets a value indicating whether this instance view is initialized.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance view is initialized; otherwise, <c>false</c>.
        /// </value>
        public bool IsViewInitialized { get; private set; }

        /// <summary>
        /// Gets or sets the colors.
        /// </summary>
        /// <value>
        /// The colors.
        /// </value>
        public IList<Color> Colors
        {
            get => _colors ?? LiveChartsSettings.Current.Colors;
            set => _colors = value;
        }

        /// <summary>
        /// Gets or sets the default legend orientation.
        /// </summary>
        /// <value>
        /// The default legend orientation.
        /// </value>
        public Orientation DefaultLegendOrientation =>
            LegendPosition == LegendPosition.Top || LegendPosition == LegendPosition.Bottom
                ? Orientation.Horizontal
                : Orientation.Vertical;

        /// <summary>
        /// Occurs before a chart update is called.
        /// </summary>
        public event ChartEventHandler UpdatePreview;

        /// <summary>
        /// Occurs before a chart update is called.
        /// </summary>
        public ICommand UpdatePreviewCommand { get; set; }

        /// <summary>
        /// Occurs after a chart update was called.
        /// </summary>
        public event ChartEventHandler Updated;

        /// <summary>
        /// Occurs after a chart update was called.
        /// </summary>
        public ICommand UpdatedCommand { get; set; }

        internal BaseSeries[] Series { get; set; }

        internal Plane[][] Dimensions { get; set; }

        internal Size ControlSize { get; set; }

        internal Margin DrawMargin { get; set; }

        internal TimeSpan AnimationsSpeed { get; set; }

        internal IDataToolTip ToolTip { get; set; }

        internal ILegend Legend { get; set; }

        internal LegendPosition LegendPosition { get; set; }

        internal Timer TooltipTimoutTimer { get; set; }

        /// <summary>
        /// Invalidates this instance, the chart will queue an update request.
        /// </summary>
        /// <returns></returns>
        public async void Invalidate(object sender)
        {
            InvalidateCount++;
            if (!IsViewInitialized) return;
            if (_delayer != null && !_delayer.IsCompleted) return;
            var delay = AnimationsSpeed.TotalMilliseconds < 10
                ? TimeSpan.FromMilliseconds(10)
                : AnimationsSpeed;
            _delayer = Task.Delay(delay);
            await _delayer;
            Update(false);
        }

        /// <summary>
        /// Scales to pixels a data value according to an axis range and a given area, if the area is not present, the chart draw margin size will be used.
        /// </summary>
        /// <param name="dataValue">The value.</param>
        /// <param name="plane">The axis.</param>
        /// <param name="size">The draw margin, this param is optional, if not set, the current chart's draw margin area will be used.</param>
        /// <returns></returns>
        public abstract double ScaleToUi(double dataValue, Plane plane, Size? size = null);

        /// <summary>
        /// Scales from pixels to a data value.
        /// </summary>
        /// <param name="pixelsValue">The value.</param>
        /// <param name="plane">The plane.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public abstract double ScaleFromUi(double pixelsValue, Plane plane, Size? size = null);

        /// <summary>
        /// Scales a point to the UI.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// /// <param name="size">The draw margin, this param is optional, if not set, the current chart's draw margin area will be used.</param>
        /// <returns></returns>
        public Point2D ScaleToUi(Point2D point, Plane x, Plane y, Size? size = null)
        {
            return new Point2D(ScaleToUi(point.X, x, size), ScaleToUi(point.Y, y, size));
        }

        /// <summary>
        /// Scales to UI.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="size">The draw margin, this param is optional, if not set, the current chart's draw margin area will be used.</param>
        /// <returns></returns>
        public Point ScaleToUi(Point point, Plane x, Plane y, Size? size = null)
        {
            return new Point(ScaleToUi(point.X, x, size), ScaleToUi(point.Y, y, size));
        }

        /// <summary>
        /// Called when the pointer moves over a chart and there is a tooltip in the view.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="selectionMode">The selection mode.</param>
        /// <param name="dimensions">The dimensions.</param>
        protected abstract void ViewOnPointerMoved(
            Point location, TooltipSelectionMode selectionMode, params double[] dimensions);

        /// <summary>
        /// Updates the chart.
        /// </summary>
        /// <param name="restart">if set to <c>true</c> all the elements in the view will be redrawn.</param>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual void Update(bool restart)
        {
            UpdateId = new object();

            if (!IsViewInitialized)
            {
                Invalidate(View);
                return;
            }

            if (restart)
            {
                foreach (var resource in _resources)
                {
                    resource.Dispose(View);
                }

                _resources.Clear();
            }

            CopyDataFromView();

            // [ x: [x1: range, x2: range, x3: range, ..., xn: range], y: [...], z[...], w[...] ]
            DataRangeMatrix = Dimensions.Select(
                    x => x.Select(
                            y => new DimensionRange(
                                double.IsNaN(y.MinValue) ? double.PositiveInfinity : y.MinValue,
                                double.IsNaN(y.MaxValue) ? double.NegativeInfinity : y.MaxValue))
                        .ToArray())
                .ToArray();

            foreach (var series in Series.Where(x => x.IsVisible))
            {
                series.Fetch(this);
                RegisterResource(series);
                var x = Dimensions[0][series.ScalesAt[0]];
                var y = Dimensions[1][series.ScalesAt[1]];
                if (x.PointWidth == Point.Empty) x.ActualPointWidth = series.DefaultPointWidth;
                if (y.PointWidth == Point.Empty) y.ActualPointWidth = series.DefaultPointWidth;
            }

            var chartSize = ControlSize;
            double dax = 0, day = 0;
            double lw = 0, lh = 0;

            // draw and measure legend
            if (Legend != null && LegendPosition != LegendPosition.None)
            {
                RegisterResource(Legend);

                var legendSize = Legend.Measure(Series, DefaultLegendOrientation, View);

                double lx = 0, ly = 0;

                switch (LegendPosition)
                {
                    case LegendPosition.None:
                        break;
                    case LegendPosition.Top:
                        day = legendSize.Height;
                        lh = legendSize.Height;
                        lx = ControlSize.Width * .5 - legendSize.Width * .5;
                        break;
                    case LegendPosition.Bottom:
                        lh = legendSize.Height;
                        lx = chartSize.Width * .5 - legendSize.Width * .5;
                        ly = chartSize.Height - legendSize.Height;
                        break;
                    case LegendPosition.Left:
                        dax = legendSize.Width;
                        lw = legendSize.Width;
                        ly = chartSize.Height * .5 - legendSize.Height * .5;
                        break;
                    case LegendPosition.Right:
                        lw = legendSize.Width;
                        lx = chartSize.Width - legendSize.Width;
                        ly = chartSize.Height * .5 - legendSize.Height * .5;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Legend.Move(new Point(lx, ly), View);
            }

            DrawAreaLocation = new Point(dax, day);
            DrawAreaSize = chartSize - new Size(lw, lh);
        }

        internal void OnUpdateStarted()
        {
            UpdatePreview?.Invoke(View);
            if (UpdatePreviewCommand != null && UpdatePreviewCommand.CanExecute(View))
            {
                UpdatePreviewCommand.Execute(View);
            }
        }

        internal void OnUpdateFinished()
        {
            Updated?.Invoke(View);
            if (UpdatedCommand != null && UpdatedCommand.CanExecute(View))
            {
                UpdatedCommand.Execute(View);
            }
        }

        internal Color GetNextColor()
        {
            if (Series.Length - 1 < _colorCount) _colorCount = 0;
            return Colors[_colorCount++ % Colors.Count];
        }

        internal void RegisterResource(IResource resource)
        {
            if (!_resources.Contains(resource))
            {
                _resources.Add(resource);
                if (resource is INotifyPropertyChanged inpc)
                {
                    inpc.PropertyChanged += InvalidatePropertyChanged;

                    void DisposePropertyChanged(IChartView view, object instance)
                    {
                        inpc.PropertyChanged -= InvalidatePropertyChanged;
                        resource.Disposed -= DisposePropertyChanged;
                    }

                    resource.Disposed += DisposePropertyChanged;
                }

                if (resource is INotifyCollectionChanged incc)
                {
                    incc.CollectionChanged += InvalidateOnCollectionChanged;

                    void DisposeCollectionChanged(IChartView view, object instance)
                    {
                        incc.CollectionChanged -= InvalidateOnCollectionChanged;
                        resource.Disposed -= DisposeCollectionChanged;
                    }

                    resource.Disposed += DisposeCollectionChanged;
                }
            }

            resource.UpdateId = UpdateId;
        }

        internal void RegisterResourceCollection(string propertyName, object collection)
        {
            if (!_resourcesCollections.TryGetValue(propertyName, out var previous))
            {
                _resourcesCollections.Add(propertyName, collection);
            }
            if (previous != null && Equals(collection, previous)) return;
            if (collection is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += InvalidateOnCollectionChanged;
            }

            if (previous is INotifyCollectionChanged pincc)
            {
                pincc.CollectionChanged -= InvalidateOnCollectionChanged;
            }

            _resourcesCollections[propertyName] = collection;
        }
        
        internal void CollectResources(bool collectAll = false)
        {
            foreach (var resource in _resources.ToArray())
            {
                if (!collectAll && resource.UpdateId == UpdateId) continue;
                resource.Dispose(View);
                _resources.Remove(resource);
            }
        }

        private void InvalidatePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            Invalidate(View);
        }

        private void InvalidateOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            Invalidate(View);
        }

        private void CopyDataFromView()
        {
            Series = (View.Series ?? Enumerable.Empty<BaseSeries>()).ToArray();
            Dimensions = View.Dimensions.Select(x => x.ToArray()).ToArray();
            ControlSize = View.ControlSize;
            DrawMargin = View.DrawMargin;
            AnimationsSpeed = View.AnimationsSpeed;
            LegendPosition = View.LegendPosition;
            Legend = View.Legend;

            RegisterResourceCollection(nameof(IChartView.Series), View.Series);
            for (var index = 0; index < View.Dimensions.Count; index++)
            {
                var dimension = View.Dimensions[index];
                RegisterResourceCollection($"{nameof(IChartView.Dimensions)}[{index}]", dimension);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var resource in _resources)
            {
                resource.Dispose(View);
            }

            foreach (var resourceCollection in _resourcesCollections.Values)
            {
                if (resourceCollection is INotifyCollectionChanged incc)
                {
                    incc.CollectionChanged -= InvalidateOnCollectionChanged;
                }
            }
        }
    }
}