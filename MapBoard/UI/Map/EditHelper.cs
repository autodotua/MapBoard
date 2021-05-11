using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.UI.Dialog;
using MapBoard.Common.Resource;
using MapBoard.Main.Model;
using MapBoard.Main.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MapBoard.Main.UI.Map
{
    public class EditHelper
    {
        public ArcMapView Mapview => ArcMapView.Instance;

        /// <summary>
        /// 编辑
        /// </summary>
        public async void StartEdit(EditMode mode)
        {
            Mode = mode;

            editingFeature = Mapview.Selection.SelectedFeatures.First();
            Mapview.Drawing.Attributes = FeatureAttributes.FromFeature(editingFeature);

            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Edit;
            if (mode == EditMode.Draw)
            {
                await editingFeature.FeatureTable.DeleteFeatureAsync(editingFeature);
                await Mapview.SketchEditor.StartAsync(GeometryEngine.Project(editingFeature.Geometry, SpatialReferences.WebMercator));
            }
            else if (mode == EditMode.Cut)
            {
                await Mapview.SketchEditor.StartAsync(Esri.ArcGISRuntime.UI.SketchCreationMode.Polyline);
            }
        }

        private void StopCut()
        {
            var geometry = Mapview.SketchEditor.Geometry;
            if (geometry != null)
            {
                Polyline line = geometry as Polyline;
                if (line.Parts[0].PointCount > 1)
                {
                    editingFeature.FeatureTable.DeleteFeatureAsync(editingFeature);
                    var newPolylines = GeometryEngine.Cut(editingFeature.Geometry, GeometryEngine.Project(line, SpatialReferences.Wgs84) as Polyline);
                    int partsCount = 0;
                    foreach (var newPolyline in newPolylines)
                    {
                        if (newPolyline.GeometryType == GeometryType.Polyline)
                        {
                            foreach (var part in (newPolyline as Polyline).Parts)
                            {
                                partsCount++;
                                Feature feature = editingFeature.FeatureTable.CreateFeature();
                                feature.Geometry = new Polyline(part);
                                editingFeature.FeatureTable.AddFeatureAsync(feature);
                            }
                        }
                        else if (newPolyline.GeometryType == GeometryType.Polygon)
                        {
                            foreach (var part in (newPolyline as Polygon).Parts)
                            {
                                partsCount++;
                                Feature feature = editingFeature.FeatureTable.CreateFeature();
                                feature.Geometry = new Polygon(part);
                                editingFeature.FeatureTable.AddFeatureAsync(feature);
                            }
                        }
                    }

                    SnakeBar.Show("成功分割为" + partsCount + "部分");
                    LayerCollection.Instance.Selected.UpdateFeatureCount();
                }
            }

            Mapview.SketchEditor.Stop();
            Mapview.Selection.ClearSelection();
        }

        public enum EditMode
        {
            Draw,
            Cut
        }

        public EditMode Mode { get; private set; }

        // private SnakeBar lastBar;
        private Feature editingFeature;

        public async Task DeleteSelectedFeatures()
        {
            HashSet<FeatureTable> tables = new HashSet<FeatureTable>();
            foreach (var feature in Mapview.Selection.SelectedFeatures)
            {
                tables.Add(feature.FeatureTable);
                try
                {
                    await feature.FeatureTable.DeleteFeatureAsync(feature);
                }
                catch (Exception ex)
                {
                    SnakeBar.ShowException(ex);
                }
            }
            foreach (var table in tables)
            {
                LayerCollection.Instance.Layers.First(p => p.Table == table).UpdateFeatureCount();
            }
            Mapview.Selection.SelectedFeatures.Clear();
        }

        public async Task StopEditing()
        {
            if (Mode == EditMode.Draw)
            {
                await Mapview.Drawing.StopDraw();
                await Mapview.Selection.StopFrameSelect(false);
            }
            else if (Mode == EditMode.Cut)
            {
                StopCut();
            }

            Mapview.Selection.ClearSelection();
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;
        }

        public async Task AbandonEditing()
        {
            Mapview.SketchEditor.Stop();
            if (Mode == EditMode.Draw)
            {
                await editingFeature.FeatureTable.AddFeatureAsync(editingFeature);
                LayerCollection.Instance.Selected.UpdateFeatureCount();
            }

            Mapview.Selection.ClearSelection();
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;
        }
    }
}