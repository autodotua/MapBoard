﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.UI.Editing;
using FzLib;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using MapBoard.Util;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Symbology;
using NGettext.Loaders;
using Esri.ArcGISRuntime.Mapping.Popups;
using System.Text;
using MapBoard.Views;

namespace MapBoard.Mapping
{
    /// <summary>
    /// 主地图画板地图
    /// </summary>
    public class MainMapView : MapView
    {
        /// <summary>
        /// 画板当前任务
        /// </summary>
        private static BoardTask currentTask = BoardTask.NotReady;

        /// <summary>
        /// 所有<see cref="MainMapView"/>实例
        /// </summary>
        private static List<MainMapView> instances = new List<MainMapView>();

        private readonly double watermarkHeight = 72;


        private bool isZoomingToLastExtent = false;

        private Feature selectedFeature = null;

        public MainMapView()
        {
            if (instances.Count > 1)
            {
                throw new Exception("该类仅支持单例");
            }
            instances.Add(this);
            Layers = new MapLayerCollection();

            IsAttributionTextVisible = false;
            Margin = new Thickness(-watermarkHeight);

            InteractionOptions = new MapViewInteractionOptions()
            {
                IsRotateEnabled = true
            };
            PropertyChanged += MainMapView_PropertyChanged;

            //启动时恢复到原来的视角，并定时保存
            Loaded += MainMapView_Loaded;
            Unloaded += MainMapView_Unloaded;
            GeoViewTapped += MainMapView_GeoViewTapped;

            Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (IsLoaded
                    && Layers != null
                 && GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry is Envelope envelope)
                {
                    Layers.MapViewExtentJson = envelope.ToJson();
                }
                return true;
            });
            //NavigationCompleted += MainMapView_NavigationCompleted;
        }


        /// <summary>
        /// 画板当前任务改变事件
        /// </summary>
        public event EventHandler<BoardTaskChangedEventArgs> BoardTaskChanged;

        public event EventHandler MapLoaded;

        public static MainMapView Current => instances[0];

        /// <summary>
        /// 所有<see cref="MainMapView"/>实例
        /// </summary>
        public static IReadOnlyList<MainMapView> Instances => instances.AsReadOnly();

        /// <summary>
        /// 底图加载错误
        /// </summary>
        public ItemsOperationErrorCollection BaseMapLoadErrors { get; private set; }

        /// <summary>
        /// 画板当前任务
        /// </summary>
        public BoardTask CurrentTask
        {
            get => currentTask;
            set
            {
                if (currentTask != value)
                {
                    BoardTask oldTask = currentTask;
                    currentTask = value;

                    BoardTaskChanged?.Invoke(null, new BoardTaskChangedEventArgs(oldTask, value));
                }
            }
        }

        /// <summary>
        /// 图层
        /// </summary>
        public MapLayerCollection Layers { get; }

        /// <summary>
        /// 编辑器相关
        /// </summary>
        //public EditorHelper Editor { get; private set; }
        public TrackOverlayHelper TrackOverlay { get; private set; }

        /// <summary>
        /// 选择相关
        /// </summary>
        //public SelectionHelper Selection { get; private set; }
        /// <summary>
        /// 初始化加载
        /// </summary>
        /// <returns></returns>
        public async Task LoadAsync()
        {
            BaseMapLoadErrors = await MapViewHelper.LoadBaseGeoViewAsync(this, Config.Instance.EnableBasemapCache);
            Map.MaxScale = Config.Instance.MaxScale;
            Map.OperationalLayers.Clear();
            await Layers.LoadAsync(Map.OperationalLayers);
            CurrentTask = BoardTask.Ready;
            MapLoaded?.Invoke(this, EventArgs.Empty);

            if (TrackOverlay == null)
            {
                var overlay = new GraphicsOverlay()
                {
                    Renderer = new SimpleRenderer(new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.FromArgb(0x54, 0xA5, 0xF6), 6))
                };
                GraphicsOverlays.Add(overlay);
                TrackOverlay = new TrackOverlayHelper(overlay);
            }
        }

        public void MoveToLocation()
        {
            if (LocationDisplay != null && LocationDisplay.IsEnabled)
            {
                var point = LocationDisplay.MapLocation;
                if (point != null)
                {
                    SetViewpointCenterAsync(point);
                }
            }
        }

        private string BuildCalloutText(Feature feature)
        {
            var attrStr = new StringBuilder();

            switch (feature.Geometry.GeometryType)
            {
                case GeometryType.Point:
                    break;
                case GeometryType.Envelope:
                    break;
                case GeometryType.Polyline:
                    double length = (feature.Geometry as Polyline).GetLength();
                    if (length < 1000)
                    {
                        attrStr.AppendLine($"{length:0.0} m");
                    }
                    else
                    {
                        attrStr.AppendLine($"{length / 1000:0.00} km");
                    }
                    break;
                case GeometryType.Polygon:
                    double area = (feature.Geometry as Polygon).GetLength();
                    if (area < 1_000_000)
                    {
                        attrStr.AppendLine($"{area:0.0} m²");
                    }
                    else
                    {
                        attrStr.AppendLine($"{area / 1_000_000:0.00} km²");
                    }
                    break;
                case GeometryType.Multipoint:
                    break;
                case GeometryType.Unknown:
                    break;
            }

            Dictionary<string, string> key2Desc = Layers
                .First(p => (p as MapLayerInfo).Layer == feature.FeatureTable.Layer)
                .Fields
                .ToDictionary(p => p.Name, p => p.DisplayName);
            foreach (var kv in feature.Attributes)
            {

                if (FieldExtension.IsIdField(kv.Key) || kv.Key is (Parameters.CreateTimeFieldName or Parameters.ModifiedTimeFieldName))
                {
                    continue;
                }

                if (kv.Value is not (null or ""))
                {
                    if (key2Desc.TryGetValue(kv.Key, out var value))
                    {
                        attrStr.Append(value);
                    }
                    else
                    {
                        attrStr.Append(kv.Key);
                    }

                    attrStr.Append('：');
                    if (kv.Value is DateTimeOffset dto)
                    {
                        attrStr.AppendLine(dto.DateTime.ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        attrStr.AppendLine(kv.Value.ToString());
                    }
                }
            }

            return attrStr.ToString().Trim();
        }

        public void ClearSelection()
        {
            DismissCallout();
            if (selectedFeature != null)
            {
                (selectedFeature.FeatureTable.Layer as FeatureLayer).UnselectFeature(selectedFeature);
                selectedFeature = null;
            }
        }

        private async void MainMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            ClearSelection();
            try
            {
                var result = Layers.Selected == null ? null : await IdentifyLayerAsync(Layers.Selected.Layer, e.Position, 10, false, 1);
                if (result != null && result.GeoElements.Count > 0)
                {
                    SelectFeature(result.GeoElements[0] as Feature, e.Position);
                }
                else
                {
                    var results = await IdentifyLayersAsync(e.Position, 10, false, 1);
                    if (results.Any())
                    {
                        //下面三条语句用于寻找各图层中最近的那个
                        var features = results.Select(p => p.GeoElements[0] as Feature).ToList();
                        var distances = features.Select(p => GeometryEngine.NearestCoordinate(p.Geometry, e.Location.ToWgs84())?.Distance ?? double.MaxValue).ToList();
                        var nearestFeatureIndex = distances.IndexOf(distances.Min());
                        SelectFeature(features[nearestFeatureIndex], e.Position);
                    }
                }
            }
            catch (Exception ex)
            {
                await MainPage.Current.DisplayAlert("选取失败", ex.Message, "确定");
            }
        }

        private async void MainMapView_Loaded(object sender, EventArgs e)
        {
            if (Map == null)
            {
                await LoadAsync();
            }
            if (!isZoomingToLastExtent)
            {
                isZoomingToLastExtent = true;
                await this.TryZoomToLastExtent();
                isZoomingToLastExtent = false;
            }
        }

        private void MainMapView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocationDisplay))
            {
                if (LocationDisplay != null)
                {
                    LocationDisplay.NavigationPointHeightFactor = 0.4;
                    LocationDisplay.WanderExtentFactor = 0;
                    LocationDisplay.IsEnabled = true;
                }
            }
        }

        private void MainMapView_Unloaded(object sender, EventArgs e)
        {
            Config.Instance.Save();
            Layers.Save();
        }

        private readonly byte[] closeImage = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEU" +
            "gAAADAAAAAwCAYAAABXAvmHAAAABHNCSVQICAgIfAhkiAAAAL" +
            "9JREFUaIHt10sOgzAQA1CrJ/WNyk3LDdpNs0G0JDMOZVQ/iRXEYyS+" +
            "gJmZmZmNWgA8AVCQxXfWXZDVpZVvGxNZ3GQtyW5d1s3Q6ElwJ2e" +
            "VNAwMHj0JRUZKpkBmrVSkSGTNVCOFRo49FXFcrOeYnyI+F/y271KI/aIlyj" +
            "dE4fINcWL5mzqwOqLwJUQUvomJwo9RovCLjCj8KREpFFkzRaZIZq2EooAiI" +
            "2zmL+VD0nBwMC+SFRqsGKjMMjMzM/srL6TW4v8zOk00AAAAAElFTkSuQmCC");
        private void SelectFeature(Feature feature, Point point)
        {
            (feature.FeatureTable.Layer as FeatureLayer).ClearSelection();
            (feature.FeatureTable.Layer as FeatureLayer).SelectFeature(feature);
            CalloutDefinition cd = new CalloutDefinition(feature)
            {
                DetailText = BuildCalloutText(feature),
                Text = feature.FeatureTable.Layer.Name,
                ButtonImage = new RuntimeImage(closeImage)
            };
            cd.OnButtonClick = a =>
            {
                ClearSelection();
            };
            ShowCalloutForGeoElement(feature, point, cd);
            selectedFeature = feature;
        }

    }
}