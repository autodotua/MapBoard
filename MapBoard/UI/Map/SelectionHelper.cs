using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using FzLib.Basic;
using FzLib.Basic.Collection;
using MapBoard.Main.Model;
using MapBoard.Main.UI;
using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using LayerCollection = MapBoard.Main.Model.LayerCollection;

namespace MapBoard.Main.UI.Map
{
    public class SelectionHelper
    {
        private SelectionHelper instance;

        public SelectionHelper()
        {
            instance = this;
            Mapview.GeoViewTapped += MapviewTapped;
            SelectedFeatures.CollectionChanged += (s, e) =>
              {
                  if (SelectedFeatures.Count == 0 && BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select)
                  {
                      BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;
                  }
                  else if (SelectedFeatures.Count != 0 && BoardTaskManager.CurrentTask != BoardTaskManager.BoardTask.Select)
                  {
                      BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Select;
                  }
              };
        }

        private async void MapviewTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Draw || LayerCollection.Instance.Selected == null || !LayerCollection.Instance.Selected.LayerVisible || BoardTaskManager.CurrentTask != BoardTaskManager.BoardTask.Select && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                return;
            }
            if (BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select)
            {
                Mapview.SketchEditor.Stop();
            }
            MapPoint point = GeometryEngine.Project(e.Location, SpatialReferences.Wgs84) as MapPoint;
            double tolerance = Mapview.MapScale / 1e8;
            Envelope envelope = new Envelope(point.X - tolerance, point.Y - tolerance, point.X + tolerance, point.Y + tolerance, SpatialReferences.Wgs84);

            await Select(envelope, e.Position, SpatialRelationship.Intersects);
        }

        public ArcMapView Mapview => ArcMapView.Instance;

        public async Task StartSelect(SketchCreationMode mode)
        {
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Select;
            //Mapview.SketchEditor.Stop();
            await Mapview.SketchEditor.StartAsync(mode);
        }

        public void ClearSelection()
        {
            foreach (var layer in SelectedFeatures.Select(p => p.FeatureTable.FeatureLayer).ToHashSet())
            {
                layer.ClearSelection();
            }
            SelectedFeatures.Clear();
        }

        public async Task StopFrameSelect(bool save)
        {
            ClearSelection();
            if (save)
            {
                Envelope envelope = Mapview.SketchEditor.Geometry == null ? null : (Mapview.SketchEditor.Geometry as Polygon).Extent;
                envelope = GeometryEngine.Project(envelope, SpatialReferences.Wgs84) as Envelope;
                await Select(envelope, null, SpatialRelationship.Contains);
            }

            Mapview.SketchEditor.Stop();

            //BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;
        }

        public async Task Select(Envelope envelope, System.Windows.Point? point, SpatialRelationship relationship)
        {
            await (App.Current.MainWindow as MainWindow).DoAsync(async () =>
             {
                 if (envelope == null)
                 {
                     return;
                 }

                 bool multiple = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
                 bool inverse = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
                 QueryParameters query = new QueryParameters();
                 query.Geometry = envelope;
                 query.SpatialRelationship = relationship;
                 FeatureLayer layer = LayerCollection.Instance.Selected?.Layer;
                 if (layer == null)
                 {
                     return;
                 }
                 //多图层选择过于复杂，不进行实现
                 //if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
                 //&& Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)
                 //&& point.HasValue)
                 //{
                 //    var result = await Mapview.IdentifyLayersAsync(point.Value, 10, false);
                 //    foreach (var r in result)
                 //    {
                 //        if (r.LayerContent is FeatureLayer l)
                 //        {
                 //            l.SelectFeatures(r.GeoElements.Cast<Feature>());
                 //            SelectedFeatures.AddRange(r.GeoElements.Cast<Feature>());
                 //        }
                 //    }
                 //}
                 //else
                 //{
                 SelectionMode mode = SelectionMode.New;
                 if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                 {
                     mode = SelectionMode.Add;
                 }
                 else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                 {
                     mode = SelectionMode.Subtract;
                 }
                 var result = await layer.SelectFeaturesAsync(query, mode);
                 List<Feature> features = null;
                 await Task.Run(() =>
                 {
                     features = result.ToList();
                 });
                 if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                 {
                     features = features.Where(p =>
                     !SelectedFeatures.Any(q => p.GetAttributeValue("FID")
                     .Equals(q.GetAttributeValue("FID")))).ToList();
                     SelectedFeatures.AddRange(features);
                 }
                 else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                 {
                     SelectedFeatures.RemoveRange(features);
                 }
                 else
                 {
                     SelectedFeatures.Clear();
                     SelectedFeatures.AddRange(features);
                 }
                 // }
                 // FeatureQueryResult result = await layer.query layer.FeatureTable.QueryFeaturesAsync(query);
                 // List<Feature> features = null;
                 // await Task.Run(() =>
                 //{
                 //    features = result.ToList();
                 //});
                 // if (features.Count != 0)
                 // {
                 //     if (!multiple)
                 //     {
                 //         ClearSelection();
                 //     }
                 //     foreach (var feature in features)
                 //     {
                 //         if (SelectedFeatures.Any(p => p.Geometry.ToJson() == feature.Geometry.ToJson()))
                 //         {
                 //             if (inverse)
                 //             {
                 //                 layer.UnselectFeature(feature);
                 //                 SelectedFeatures.Remove(SelectedFeatures.First(p => p.Geometry.ToJson() == feature.Geometry.ToJson()));
                 //             }
                 //         }
                 //         else
                 //         {
                 //             layer.SelectFeature(feature);
                 //             SelectedFeatures.Add(feature);
                 //         }
                 //     }
                 // }
             });
        }

        public ExtendedObservableCollection<Feature> SelectedFeatures { get; } = new ExtendedObservableCollection<Feature>();

        public void Select(Feature feature, bool clearAll = false)
        {
            var layer = feature.FeatureTable.FeatureLayer;
            if (layer == null)
            {
                return;
            }
            if (clearAll && SelectedFeatures.Count > 0)
            {
                foreach (var l in SelectedFeatures.Select(p => p.FeatureTable.FeatureLayer).Distinct().ToArray())
                {
                    l.ClearSelection();
                }
                SelectedFeatures.Clear();
            }

            layer.SelectFeature(feature);
            ArcMapView.Instance.Selection.SelectedFeatures.Add(feature);
        }

        public void Select(IEnumerable<Feature> features)
        {
            var layer = LayerCollection.Instance.Selected?.Layer;
            if (layer == null)
            {
                return;
            }
            layer.SelectFeatures(features);
            ArcMapView.Instance.Selection.SelectedFeatures.AddRange(features);
        }
    }
}