﻿using System.Collections.Generic;
using System.Windows;
using LiveCharts.Core.Abstractions;
using LiveCharts.Core.Charts;
using LiveCharts.Core.Collections;
using LiveCharts.Core.DataSeries;
using LiveCharts.Core.Dimensions;

namespace LiveCharts.Wpf
{
    public class CartesianChart : Chart
    {
         public CartesianChart()
        {
            Model = new CartesianChartModel(this);
            SetValue(SeriesProperty, new PlotableCollection<BaseSeries>());
            SetValue(XAxisProperty, new PlotableCollection<Plane> {new Axis()});
            SetValue(YAxisProperty, new PlotableCollection<Plane> {new Axis()});
        }

        #region Dependency properties

        /// <summary>
        /// The x axis property.
        /// </summary>
        public static readonly DependencyProperty XAxisProperty = DependencyProperty.Register(
            nameof(XAxis), typeof(IList<Plane>), typeof(CartesianChart),
            new PropertyMetadata(null, RaiseOnPropertyChanged(nameof(IChartView.Dimensions))));

        /// <summary>
        /// The y axis property
        /// </summary>
        public static readonly DependencyProperty YAxisProperty = DependencyProperty.Register(
            nameof(YAxis), typeof(IList<Plane>), typeof(CartesianChart),
            new PropertyMetadata(null, RaiseOnPropertyChanged(nameof(IChartView.Dimensions))));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the x axis.
        /// </summary>
        /// <value>
        /// The x axis.
        /// </value>
        public IList<Plane> XAxis
        {
            get => (IList<Plane>) GetValue(XAxisProperty);
            set => SetValue(XAxisProperty, value);
        }

        /// <summary>
        /// Gets or sets the y axis.
        /// </summary>
        /// <value>
        /// The y axis.
        /// </value>
        public IList<Plane> YAxis
        {
            get => (IList<Plane>) GetValue(YAxisProperty);
            set => SetValue(YAxisProperty, value);
        }

        #endregion

        /// <inheritdoc cref="Chart.GetPlanes"/>
        protected override IList<IList<Plane>> GetPlanes()
        {
            return new List<IList<Plane>>
            {
                XAxis,
                YAxis
            };
        }
    }
}