using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using MapBoard.Code;
using MapBoard.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MapBoard.BoardOperation
{
    public class SelectionHelper
    {
        private SelectionHelper instance;
        public SelectionHelper(ArcMapView mapview)
        {
            instance = this;
            Mapview = mapview ?? throw new ArgumentNullException(nameof(mapview));
            Mapview.GeoViewTapped += MapviewTapped;
        }



        private async void MapviewTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (!IsSelecting && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                return;
            }
            MapPoint point = GeometryEngine.Project(e.Location,SpatialReferences.Wgs84) as MapPoint;
            double tolerance = Mapview.MapScale / 1e8;
            Envelope envelope = new Envelope(point.X - tolerance, point.Y - tolerance, point.X + tolerance, point.Y + tolerance, SpatialReferences.Wgs84);
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                ClearSelection();
            }
            await Select(envelope, SpatialRelationship.Intersects);
        }

        public ArcMapView Mapview { get; private set; }
        public async Task StartSelect(SketchCreationMode mode)
        {
            IsSelecting = true;
            //Mapview.SketchEditor.Stop();
            await Mapview.SketchEditor.StartAsync(mode);
        }

        private void ClearSelection()
        {
            foreach (var layer in SelectedFeatures.Select(p => p.FeatureTable.FeatureLayer).ToHashSet())
            {
                layer.ClearSelection();
            }
            SelectedFeatures.Clear();
        }
        public async Task StopSelect(bool save)
        {

            ClearSelection();
            if (save)
            {
                Envelope envelope = Mapview.SketchEditor.Geometry == null ? null : (Mapview.SketchEditor.Geometry as Polygon).Extent;
                envelope = GeometryEngine.Project(envelope, SpatialReferences.Wgs84) as Envelope;
                await Select(envelope, SpatialRelationship.Contains);
            }

            Mapview.SketchEditor.Stop();
            IsSelecting = false;
        }

        public async Task Select(Envelope envelope, SpatialRelationship relationship)
        {
            if (envelope == null)
            {
                return;
            }

            bool multiple = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

            QueryParameters query = new QueryParameters();
            query.Geometry = envelope;
            query.SpatialRelationship = relationship;
            //foreach (FeatureLayer layer in Mapview.Map.OperationalLayers)
            //{

            var layer = StyleCollection.Instance.Selected?.Layer;
            if (layer == null)
            {
                return;
            }
            FeatureQueryResult result = await layer.FeatureTable.QueryFeaturesAsync(query);
            foreach (var feature in result)
            {
                layer.SelectFeature(feature);
                SelectedFeatures.Add(feature);
            }
            //}
            
        }

        public event EventHandler SelectingStatusChanged;
        private bool isSelecting = false;
        public bool IsSelecting
        {
            get => isSelecting;
            set
            {
                if(isSelecting!=value)
                {
                    SelectingStatusChanged?.Invoke(this, new EventArgs());
                    isSelecting = value;
                }
            }
        }
        public ObservableCollection<Feature> SelectedFeatures { get; } = new ObservableCollection<Feature>();
    }
}
