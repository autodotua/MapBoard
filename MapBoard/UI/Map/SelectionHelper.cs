using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Basic.Collection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MapBoard.Main.UI.Map
{
    public class SelectionHelper
    {
        private bool isClearing = false;

        public SelectionHelper(ArcMapView map)
        {
            map.GeoViewTapped += MapviewTapped;
            SelectedFeatures.CollectionChanged += SelectedFeatures_CollectionChanged;
            MapView = map;
            Editor.EditorStatusChanged += Editor_EditorStatusChanged;
        }

        public EditorHelper Editor => MapView.Editor;

        public MapLayerCollection Layers => MapView.Layers;

        public ArcMapView MapView { get; }

        public ExtendedObservableCollection<Feature> SelectedFeatures { get; } = new ExtendedObservableCollection<Feature>();

        public void ClearSelection()
        {
            isClearing = true;
            Editor.Cancel();
            foreach (var layer in SelectedFeatures.Select(p => p.FeatureTable.Layer as FeatureLayer).ToHashSet())
            {
                layer.ClearSelection();
            }
            SelectedFeatures.Clear();
            isClearing = false;
        }

        public bool Select(Feature feature, bool clearAll = false)
        {
            var layer = feature.FeatureTable.Layer as FeatureLayer;
            if (layer == null)
            {
                return false;
            }
            if (clearAll && SelectedFeatures.Count > 0)
            {
                (SelectedFeatures[0].FeatureTable.Layer as FeatureLayer).ClearSelection();
                SelectedFeatures.Clear();
            }
            if (SelectedFeatures.Any(p => p.GetAttributeValue("FID").Equals(feature.GetAttributeValue("FID"))))
            {
                return false;
            }
            layer.SelectFeature(feature);
            SelectedFeatures.Add(feature);
            return true;
        }

        public void Select(IEnumerable<Feature> features)
        {
            var layer = Layers.Selected?.Layer;
            if (layer == null)
            {
                return;
            }
            layer.SelectFeatures(features);
            SelectedFeatures.AddRange(features);
        }

        public async Task SelectRectangleAsync()
        {
            ClearSelection();
            var envelope = await Editor.GetRectangleAsync();
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

        private void Editor_EditorStatusChanged(object sender, EditorStatusChangedEventArgs e)
        {
            if (!isClearing && !e.IsRunning)
            {
                ClearSelection();
            }
        }

        private async void MapviewTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (MapView.CurrentTask == BoardTask.Draw || Layers.Selected == null
                || !Layers.Selected.LayerVisible
                || MapView.CurrentTask != BoardTask.Select && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                return;
            }
            MapPoint point = GeometryEngine.Project(e.Location, SpatialReferences.Wgs84) as MapPoint;
            double tolerance = MapView.MapScale / 1e8;
            Envelope envelope = new Envelope(point.X - tolerance, point.Y - tolerance, point.X + tolerance, point.Y + tolerance, SpatialReferences.Wgs84);

            await SelectAsync(envelope, e.Position, SpatialRelationship.Intersects);
        }

        private async Task SelectAsync(Envelope envelope, System.Windows.Point? point, SpatialRelationship relationship)
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
                 FeatureLayer layer = Layers.Selected?.Layer;
                 if (layer == null)
                 {
                     return;
                 }
                 SelectionMode mode = SelectionMode.Add;
                 if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                 {
                     mode = SelectionMode.New;
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
                 if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                 {
                     SelectedFeatures.Clear();
                     SelectedFeatures.AddRange(features);
                 }
                 else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                 {
                     SelectedFeatures.RemoveRange(features);
                 }
                 else
                 {
                     features = features.Where(p =>
                       !SelectedFeatures.Any(q => p.GetAttributeValue("FID")
                       .Equals(q.GetAttributeValue("FID")))).ToList();
                     SelectedFeatures.AddRange(features);
                 }
             });
        }

        private void SelectedFeatures_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (SelectedFeatures.Count == 0 && MapView.CurrentTask == BoardTask.Select)
            {
                MapView.CurrentTask = BoardTask.Ready;
            }
            else if (SelectedFeatures.Count != 0 && MapView.CurrentTask != BoardTask.Select)
            {
                MapView.CurrentTask = BoardTask.Select;
            }
        }
    }
}