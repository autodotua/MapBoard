using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using FzLib.Basic.Collection;
using MapBoard.Main.Layer;
using MapBoard.Main.UI;
using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using LayerCollection = MapBoard.Main.Layer.LayerCollection;

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
            if (BoardTaskManager.CurrentTask== BoardTaskManager.BoardTask.Draw || LayerCollection.Instance.Selected==null || !LayerCollection.Instance.Selected.LayerVisible||  BoardTaskManager.CurrentTask != BoardTaskManager.BoardTask.Select && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                return;
            }
           if(BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select)
            {
                Mapview.SketchEditor.Stop();
            }
            MapPoint point = GeometryEngine.Project(e.Location, SpatialReferences.Wgs84) as MapPoint;
            double tolerance = Mapview.MapScale / 1e8;
            Envelope envelope = new Envelope(point.X - tolerance, point.Y - tolerance, point.X + tolerance, point.Y + tolerance, SpatialReferences.Wgs84);
           
            await Select(envelope, SpatialRelationship.Intersects);
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
                await Select(envelope, SpatialRelationship.Contains);
            }

            Mapview.SketchEditor.Stop();

            //BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;
        }


        public async Task Select(Envelope envelope, SpatialRelationship relationship)
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
            //foreach (FeatureLayer layer in Mapview.Map.OperationalLayers)
            //{

            var layer = LayerCollection.Instance.Selected?.Layer;
            if (layer == null)
            {
                return;
            }
            FeatureQueryResult result = await layer.FeatureTable.QueryFeaturesAsync(query);
            if (result.Count() != 0)
            {
                if (!multiple)
                {
                    ClearSelection();
                }
                foreach (var feature in result)
                {
                    if (SelectedFeatures.Any(p => p.Geometry.ToJson() == feature.Geometry.ToJson()))
                    {
                        if (inverse)
                        {
                            layer.UnselectFeature(feature);
                            SelectedFeatures.Remove(SelectedFeatures.First(p => p.Geometry.ToJson() == feature.Geometry.ToJson()));
                        }
                    }
                    else
                    {
                        layer.SelectFeature(feature);
                        SelectedFeatures.Add(feature);
                    }
                }
            }
            //}

        }

        //public event EventHandler SelectingStatusChanged;
        //private bool isSelecting = false;
        //public bool IsSelecting
        //{
        //    get => isSelecting;
        //    set
        //    {
        //        if(isSelecting!=value)
        //        {
        //            SelectingStatusChanged?.Invoke(this, new EventArgs());
        //            isSelecting = value;
        //        }
        //    }
        //}
        public ExtendedObservableCollection<Feature> SelectedFeatures { get; } = new ExtendedObservableCollection<Feature>();
        public void Select(Feature feature,bool clearAll=false)
        {
        
            var layer = LayerCollection.Instance.Selected?.Layer;
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
