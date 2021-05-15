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

            await SelectAsync(envelope, e.Position, SpatialRelationship.Intersects);
        }

        public ArcMapView Mapview => ArcMapView.Instance;

        public async Task SelectRectangleAsync()
        {
            ClearSelection();
            var envelope = await Mapview.Editor.GetRectangleAsync();
            if (envelope != null)
            {
                envelope = GeometryEngine.Project(envelope, SpatialReferences.Wgs84) as Envelope;
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    await SelectAsync(envelope, null, SpatialRelationship.Intersects);
                }
                else
                {
                    await SelectAsync(envelope, null, SpatialRelationship.Contains);
                }
            }
        }

        public void ClearSelection()
        {
            Mapview.Editor.Cancel();
            foreach (var layer in SelectedFeatures.Select(p => p.FeatureTable.Layer as FeatureLayer).ToHashSet())
            {
                layer.ClearSelection();
            }
            SelectedFeatures.Clear();
        }

        public async Task SelectAsync(Envelope envelope, System.Windows.Point? point, SpatialRelationship relationship)
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
             });
        }

        public ExtendedObservableCollection<Feature> SelectedFeatures { get; } = new ExtendedObservableCollection<Feature>();

        public void Select(Feature feature, bool clearAll = false)
        {
            var layer = feature.FeatureTable.Layer as FeatureLayer;
            if (layer == null)
            {
                return;
            }
            if (clearAll && SelectedFeatures.Count > 0)
            {
                foreach (var l in SelectedFeatures.Select(p => p.FeatureTable.Layer as FeatureLayer).Distinct().ToArray())
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