using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Basic;
using FzLib.Basic.Collection;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

        public Dictionary<long, Feature>.KeyCollection SelectedFeatureIDs => selectedFeatures.Keys;
        public Dictionary<long, Feature>.ValueCollection SelectedFeatures => selectedFeatures.Values;
        private Dictionary<long, Feature> selectedFeatures = new Dictionary<long, Feature>();

        public event EventHandler<SelectedFeaturesChangedEventArgs> CollectionChanged;

        public void ClearSelection()
        {
            ClearSelection(true);
        }

        private void ClearSelection(bool raiseEvent)
        {
            isClearing = true;
            Editor.Cancel();
            var layers = selectedFeatures.Select(p => p.Value.FeatureTable.Layer as FeatureLayer).Distinct().ToList();
            Debug.Assert(layers.Count == 1);
            layers[0].ClearSelection();
            SelectedFeaturesChangedEventArgs e = new SelectedFeaturesChangedEventArgs(Layers.Find(layers[0]), null, selectedFeatures.Values);
            selectedFeatures.Clear();
            isClearing = false;
            if (raiseEvent)
            {
                CollectionChanged?.Invoke(this, e);
            }
        }

        public bool Select(Feature feature, bool clearAll = false)
        {
            if (selectedFeatures.ContainsKey(feature.GetFID()))
            {
                return false;
            }
            return Select(new[] { feature }, clearAll) == 1;
        }

        /// <summary>
        /// 选取一些要素
        /// </summary>
        /// <param name="features"></param>
        /// <param name="clearAll"></param>
        /// <returns>新增选取的个数</returns>
        public int Select(IEnumerable<Feature> features, bool clearAll = false)
        {
            if (features == null || !features.Any())
            {
                throw new ArgumentException("要选择的要素为空");
            }
            Debug.Assert(features.Select(p => p.FeatureTable.Layer).Distinct().Count() == 1);
            var layer = Layers.Find(features.First().FeatureTable.Layer as FeatureLayer);
            if (layer == null)
            {
                throw new ArgumentException("找不到图层");
            }
            //如果图层不匹配，那么要先把之前图层给清除选择，保证只有一个图层被选择
            if (Layers.Selected != layer)
            {
                ClearSelection();
                Layers.Selected = layer;
            }
            List<Feature> add = new List<Feature>();
            List<Feature> remove = new List<Feature>();
            if (clearAll && SelectedFeatures.Count > 0)
            {
                remove.AddRange(SelectedFeatures);
                layer.Layer.ClearSelection();
                selectedFeatures.Clear();
            }
            add.AddRange(features);
            layer.Layer.SelectFeatures(features);
            foreach (var feature in features)
            {
                selectedFeatures.TryAdd(feature.GetFID(), feature);
            }
            CollectionChanged?.Invoke(this, new SelectedFeaturesChangedEventArgs(layer, add, remove));
            return add.Count;
        }

        public bool UnSelect(Feature feature)
        {
            if (!selectedFeatures.ContainsKey(feature.GetFID()))
            {
                return false;
            }
            return UnSelect(new[] { feature }) == 1;
        }

        /// <summary>
        /// 取消选取一些要素
        /// </summary>
        /// <param name="features"></param>
        /// <param name="clearAll"></param>
        /// <returns>新增选取的个数</returns>
        public int UnSelect(IEnumerable<Feature> features)
        {
            if (features == null || !features.Any())
            {
                throw new ArgumentException("要选择的要素为空");
            }
            Debug.Assert(features.Select(p => p.FeatureTable.Layer).Distinct().Count() == 1);
            var layer = Layers.Find(features.First().FeatureTable.Layer as FeatureLayer);
            if (layer == null)
            {
                throw new ArgumentException("找不到图层");
            }
            if (Layers.Selected != layer)
            {
                Layers.Selected = layer;
            }
            List<Feature> remove = new List<Feature>();

            layer.Layer.UnselectFeatures(features);
            foreach (var feature in features)
            {
                if (selectedFeatures.Remove(feature.GetFID()))
                {
                    remove.Add(feature);
                }
            }
            CollectionChanged?.Invoke(this, new SelectedFeaturesChangedEventArgs(layer, null, remove));
            return remove.Count;
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

        private async void MapviewTapped(object sender, GeoViewInputEventArgs e)
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
            Point? point,
            SpatialRelationship relationship,
             SelectionMode mode,
             bool allLayers = false,
             bool startEdit = false)
        {
            if (allLayers && !point.HasValue)
            {
                throw new ArgumentException("需要选取多图层，但是没有给鼠标位置");
            }
            await (Window.GetWindow(MapView) as IDoAsync).DoAsync(async () =>
             {
                 await Task.Delay(100);
                 if (envelope == null)
                 {
                     return;
                 }

                 QueryParameters query = new QueryParameters
                 {
                     Geometry = envelope,
                     SpatialRelationship = relationship
                 };
                 bool first = SelectedFeatures.Count == 0;
                 List<Feature> features = null;
                 MapLayerInfo layer = null;
                 if (allLayers)
                 {
                     IdentifyLayerResult result =
                     (await MapView.IdentifyLayersAsync(point.Value, 8, false))
                     .FirstOrDefault(p => p.LayerContent.IsVisible);
                     if (result == null)
                     {
                         return;
                     }
                     layer = Layers.Find(result.LayerContent as FeatureLayer);
                     Debug.Assert(layer != null);
                     MapView.Layers.Selected = layer;
                     features = result.GeoElements.Cast<Feature>().ToList();
                     layer.Layer.SelectFeatures(features);
                 }
                 else
                 {
                     layer = Layers.Selected;
                     FeatureLayer fLayer = Layers.Selected?.Layer;
                     if (Layers.Selected == null || !Layers.Selected.LayerVisible)
                     {
                         return;
                     }
                     FeatureQueryResult result = await fLayer.SelectFeaturesAsync(query, mode);

                     await Task.Run(() =>
                     {
                         features = result.ToList();
                     });
                 }
                 if (features.Count == 0)
                 {
                     return;
                 }
                 List<Feature> add = new List<Feature>();
                 List<Feature> remove = new List<Feature>();

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
                             add.Add(feature);
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
                                     add.Add(feature);
                                 }
                             }
                             break;

                         case SelectionMode.New:
                             remove.AddRange(selectedFeatures.Values);
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
                                     remove.Add(feature);
                                 }
                             }
                             break;

                         default:
                             throw new ArgumentOutOfRangeException();
                     }
                 }
                 CollectionChanged?.Invoke(this, new SelectedFeaturesChangedEventArgs(layer, add, remove));
             }, "正在选取");
        }

        private void SelectedFeatures_CollectionChanged(object sender, SelectedFeaturesChangedEventArgs e)
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

    public class SelectedFeaturesChangedEventArgs : EventArgs
    {
        public SelectedFeaturesChangedEventArgs(MapLayerInfo layer, IEnumerable<Feature> selected, IEnumerable<Feature> unSelected)
        {
            if (selected != null)
            {
                Selected = selected.ToArray();
            }
            else
            {
                Selected = Array.Empty<Feature>();
            }
            if (unSelected != null)
            {
                UnSelected = unSelected.ToArray();
            }
            else
            {
                UnSelected = Array.Empty<Feature>();
            }
            Layer = layer;
        }

        public Feature[] Selected { get; }
        public Feature[] UnSelected { get; }
        public MapLayerInfo Layer { get; }
    }
}