using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.UI.Editing;
using MapBoard.Mapping.Model;

namespace MapBoard.Mapping
{
    public class EditHelper
    {
        private Feature editingFeature = null;
        public Feature EditingFeature => editingFeature;

        private bool isEditing;

        public EditHelper(MainMapView mapView)
        {
            MapView = mapView;
            Editor = mapView.GeometryEditor;
        }

        public event EventHandler EditStatusChanged;

        public GeometryEditor Editor { get; }

        public bool IsEditing
        {
            get => isEditing;
            set
            {
                if (isEditing != value)
                {
                    isEditing = value;
                    EditStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool IsCreating { get; private set; }

        public MainMapView MapView { get; }

        public void Cancel()
        {
            if (!IsEditing)
            {
                throw new Exception("不在编辑状态");
            }
            editingFeature = null;
            Editor.Stop();
            IsEditing = false;
        }

        public async Task SaveAsync()
        {
            if (!IsEditing)
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
                if (IsCreating)
                {
                    await editingFeature.FeatureTable.AddFeatureAsync(editingFeature);
                }
                else
                {
                    await editingFeature.FeatureTable.UpdateFeatureAsync(editingFeature);
                }
            }
            editingFeature = null;
            IsEditing = false;
        }

        public void StartEditSelection()
        {
            if (IsEditing)
            {
                throw new Exception("正在编辑，无法再次开始编辑");
            }
            if (MapView.SelectedFeature == null)
            {
                throw new Exception("没有选择任何要素");
            }
            editingFeature = MapView.SelectedFeature;
            IsCreating = false;
            IsEditing = true;
            Editor.Start(editingFeature.Geometry);
        }

        public void StartDraw(IMapLayerInfo layer)
        {
            if (IsEditing)
            {
                throw new Exception("正在编辑，无法绘制");
            }
            IsCreating = true;
            IsEditing = true;
            editingFeature = (layer as ShapefileMapLayerInfo).CreateFeature();
            Editor.Start(layer.GeometryType);
            if (MapView.SelectedFeature != null)
            {
                MapView.ClearSelection();
            }
        }
    }
}
