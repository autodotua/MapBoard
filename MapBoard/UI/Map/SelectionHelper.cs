using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Basic;
using FzLib.Basic.Collection;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            CollectionChanged += SelectedFeatures_CollectionChanged;
            MapView = map;
            Editor.EditorStatusChanged += Editor_EditorStatusChanged;
        }

        public EditorHelper Editor => MapView.Editor;

        public MapLayerCollection Layers => MapView.Layers;

        public ArcMapView MapView { get; }

        public Dictionary<long, Feature>.ValueCollection SelectedFeatures => selectedFeatures.Values;
        private Dictionary<long, Feature> selectedFeatures = new Dictionary<long, Feature>();

        public event EventHandler CollectionChanged;

        public void ClearSelection()
        {
            isClearing = true;
            Editor.Cancel();
            Debug.Assert(MapView.Layers.Selected != null);
            MapView.Layers.Selected.Layer.ClearSelection();
            selectedFeatures.Clear();
            isClearing = false;
            CollectionChanged?.Invoke(this, new EventArgs());
        }

        public bool Select(Feature feature, bool clearAll = false)
        {
            var layer = feature.FeatureTable.Layer as FeatureLayer;
            //Debug.Assert(MapView.Layers.Selected.Layer == layer);
            if (layer == null)
            {
                return false;
            }
            if (clearAll && SelectedFeatures.Count > 0)
            {
                layer.ClearSelection();
                selectedFeatures.Clear();
            }
            if (selectedFeatures.ContainsKey(feature.GetFID()))
            {
                return false;
            }
            layer.SelectFeature(feature);
            selectedFeatures.Add(feature.GetFID(), feature);
            CollectionChanged?.Invoke(this, new EventArgs());
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
            foreach (var feature in features)
            {
                selectedFeatures.TryAdd(feature.GetFID(), feature);
            }
            CollectionChanged?.Invoke(this, new EventArgs());
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
                    await SelectAsync(envelope, null, SpatialRelationship.Intersects, SelectionMode.New);
                }
                else
                {
                    await SelectAsync(envelope, null, SpatialRelationship.Contains, SelectionMode.New);
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
            if (MapView.CurrentTask == BoardTask.Draw //正在绘制
                || (MapView.CurrentTask != BoardTask.Select)//当前不在选择状态，
                    && (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    && (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    && (!Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                    )
            {
                return;
            }
            MapPoint point = GeometryEngine.Project(e.Location, SpatialReferences.Wgs84) as MapPoint;
            double tolerance = MapView.MapScale / 1e8;
            Envelope envelope = new Envelope(point.X - tolerance, point.Y - tolerance, point.X + tolerance, point.Y + tolerance, SpatialReferences.Wgs84);
            //在“继续选择”的情况下：
            //按Ctrl表示先清除选择然后再选择
            //按Alt表示从已选择的图形中删去
            SelectionMode mode = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ?
               SelectionMode.New
               : (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) ?
               SelectionMode.Subtract
               : SelectionMode.Add);
            //在“新建选择”的情况下：
            //按Ctrl表示从当前图层中点选
            //按Shift表示从所有图层中点选
            //按Alt表示从选择后立刻进入编辑模式
            bool allLayers = MapView.CurrentTask != BoardTask.Select && Keyboard.Modifiers == ModifierKeys.Shift;
            bool edit = MapView.CurrentTask != BoardTask.Select && Keyboard.Modifiers == ModifierKeys.Alt;
            await SelectAsync(envelope, e.Position, SpatialRelationship.Intersects, mode, allLayers, edit);
        }

        private async Task SelectAsync(Envelope envelope,
            System.Windows.Point? point,
            SpatialRelationship relationship,
             SelectionMode mode,
             bool allLayers = false,
             bool startEdit = false)
        {
            if (allLayers && !point.HasValue)
            {
                throw new ArgumentException("需要选取多图层，但是没有给鼠标位置");
            }
            await (App.Current.MainWindow as MainWindow).DoAsync(async () =>
             {
                 if (envelope == null)
                 {
                     return;
                 }

                 QueryParameters query = new QueryParameters();
                 query.Geometry = envelope;
                 query.SpatialRelationship = relationship;
                 bool first = SelectedFeatures.Count == 0;
                 List<Feature> features = null;
                 if (allLayers)
                 {
                     IdentifyLayerResult result =
                     (await MapView.IdentifyLayersAsync(point.Value, 8, false))
                     .FirstOrDefault(p => p.LayerContent.IsVisible);
                     if (result == null)
                     {
                         return;
                     }
                     MapLayerInfo layer = MapView.Layers.Cast<MapLayerInfo>().FirstOrDefault(p => p.Layer == result.LayerContent);
                     Debug.Assert(layer != null);
                     MapView.Layers.Selected = layer;
                     features = result.GeoElements.Cast<Feature>().ToList();
                     layer.Layer.SelectFeatures(features);
                 }
                 else
                 {
                     FeatureLayer layer = Layers.Selected?.Layer;
                     if (Layers.Selected == null || !Layers.Selected.LayerVisible)
                     {
                         return;
                     }
                     FeatureQueryResult result = await layer.SelectFeaturesAsync(query, mode);

                     await Task.Run(() =>
                     {
                         features = result.ToList();
                     });
                 }
                 if (features.Count == 0)
                 {
                     return;
                 }

                 if (first)//首次选择
                 {
                     if (startEdit)
                     {
                         ClearSelection();
                         MapView.Editor.EditAsync(MapView.Layers.Selected, features[0]).ConfigureAwait(false);
                     }
                     else
                     {
                         foreach (var feature in features)
                         {
                             selectedFeatures.Add(feature.GetFID(), feature);
                         }
                     }
                 }
                 else//继续选择
                 {
                     switch (mode)
                     {
                         case SelectionMode.Add:
                             foreach (var feature in features)
                             {
                                 long fid = feature.GetFID();
                                 if (!selectedFeatures.ContainsKey(fid))
                                 {
                                     selectedFeatures.Add(fid, feature);
                                 }
                             }
                             break;

                         case SelectionMode.New:
                             selectedFeatures.Clear();
                             foreach (var feature in features)
                             {
                                 selectedFeatures.Add(feature.GetFID(), feature);
                             }
                             break;

                         case SelectionMode.Subtract:
                             foreach (var feature in features)
                             {
                                 long fid = feature.GetFID();
                                 if (selectedFeatures.ContainsKey(fid))
                                 {
                                     selectedFeatures.Remove(fid);
                                 }
                             }
                             break;

                         default:
                             throw new ArgumentOutOfRangeException();
                     }
                 }
                 CollectionChanged?.Invoke(this, new EventArgs());
             });
        }

        private void SelectedFeatures_CollectionChanged(object sender, EventArgs e)
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