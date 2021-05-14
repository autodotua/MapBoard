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

        public async Task<Polyline> StartCutAsync()
        {
            Mode = EditMode.Cut;

            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Edit;

            await Mapview.SketchEditor.StartAsync(Esri.ArcGISRuntime.UI.SketchCreationMode.Polyline);
            if (lastGeometry is Polyline line)
            {
                if (line.Parts[0].PointCount > 1)
                {
                    return line;
                }
                return null;
            }
            return null;
        }

        public async Task StartEditAsync()
        {
            Mode = EditMode.Draw;

            editingFeature = Mapview.Selection.SelectedFeatures.First();
            Mapview.Drawing.Attributes = FeatureAttributes.FromFeature(editingFeature);

            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Edit;

            await editingFeature.FeatureTable.DeleteFeatureAsync(editingFeature);
            await Mapview.SketchEditor.StartAsync(GeometryEngine.Project(editingFeature.Geometry, SpatialReferences.WebMercator));
        }

        public enum EditMode
        {
            Draw,
            Cut
        }

        public EditMode Mode { get; private set; }

        private Feature editingFeature;

        private Geometry lastGeometry = null;

        public async Task StopAsync()
        {
            lastGeometry = Mapview.SketchEditor.Geometry;
            if (Mode == EditMode.Draw)
            {
                await Mapview.Drawing.StopDraw();
                await Mapview.Selection.StopFrameSelect(false);
            }
            else if (Mode == EditMode.Cut)
            {
                Mapview.SketchEditor.Stop();
            }

            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;
        }

        public async Task CancelEditingAsync()
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