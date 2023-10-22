using CommunityToolkit.Maui.Alerts;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.UI.Editing;
using MapBoard.Mapping.Model;
using MapBoard.Util;

namespace MapBoard.Mapping
{
    public enum EditorStatus
    {
        NotRunning,
        Editing,
        Creating,
        Measuring
    }
    public class EditHelper
    {
        private Feature editingFeature = null;
        private EditorStatus status;
        public EditHelper(MainMapView mapView)
        {
            MapView = mapView;
            Editor = mapView.GeometryEditor;
        }

        public event EventHandler EditStatusChanged;

        public Feature EditingFeature => editingFeature;

        public GeometryEditor Editor { get; }

        public MainMapView MapView { get; }

        public EditorStatus Status
        {
            get => status;
            set
            {
                if (status != value)
                {
                    status = value;
                    EditStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public void Cancel()
        {
            if (Status is EditorStatus.NotRunning)
            {
                throw new Exception("不在编辑状态");
            }
            Editor.Stop();
            Status = EditorStatus.NotRunning;
        }

        public async Task SaveAsync()
        {
            if (Status is not EditorStatus.NotRunning)
            {
                throw new Exception("不在编辑状态");
            }
            if (editingFeature == null)
            {
                throw new Exception("没有正在编辑的要素");
            }
            var geometry = Editor.Stop();
            if (geometry != null)
            {
                editingFeature.Geometry = geometry;
                if (Status is EditorStatus.Creating)
                {
                    await editingFeature.FeatureTable.AddFeatureAsync(editingFeature);
                }
                else if (Status is EditorStatus.Creating)
                {
                    await editingFeature.FeatureTable.UpdateFeatureAsync(editingFeature);
                }
                else
                {
                    throw new NotSupportedException();
                }

            }
            editingFeature = null;
            Status = EditorStatus.NotRunning;
        }

        public void StartDraw(IMapLayerInfo layer)
        {
            if (Status is not EditorStatus.NotRunning)
            {
                throw new Exception("正在编辑，无法绘制");
            }
            Status = EditorStatus.Creating;
            editingFeature = (layer as ShapefileMapLayerInfo).CreateFeature();
            Editor.Start(layer.GeometryType);
            if (MapView.SelectedFeature != null)
            {
                MapView.ClearSelection();
            }
        }

        public void StartEditSelection()
        {
            if (Status is not EditorStatus.NotRunning)
            {
                throw new Exception("正在编辑，无法再次开始编辑");
            }
            if (MapView.SelectedFeature == null)
            {
                throw new Exception("没有选择任何要素");
            }
            editingFeature = MapView.SelectedFeature;
            Status = EditorStatus.Editing;
            Editor.Start(editingFeature.Geometry);
        }

        public void StartMeasureArea()
        {
            if (Status is not EditorStatus.NotRunning)
            {
                throw new Exception("正在编辑，无法测量");
            }
            Status = EditorStatus.Measuring;
            Editor.Start(Esri.ArcGISRuntime.Geometry.GeometryType.Polygon);
        }

        public void StartMeasureLength()
        {
            if (Status is not EditorStatus.NotRunning)
            {
                throw new Exception("正在编辑，无法测量");
            }
            Status = EditorStatus.Measuring;
            Editor.Start(Esri.ArcGISRuntime.Geometry.GeometryType.Polyline);
        }
    }
}
