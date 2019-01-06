using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Control.Dialog;
using MapBoard.Code;
using MapBoard.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.BoardOperation
{
    public class EditHelper
    {
        public EditHelper(ArcMapView mapview)
        {
            Mapview = mapview ?? throw new ArgumentNullException(nameof(mapview));
        }

        public ArcMapView Mapview { get; private set; }

        /// <summary>
        /// 编辑
        /// </summary>
        public void EditSelectedFeature()
        {
            if (Mapview.Selection.SelectedFeatures.Count > 1)
            {
                if (!Config.Instance.HideEditWarn)
                {
                    if (TaskDialog.ShowWithCheckBox(App.Current.MainWindow, "编辑时，只能同时编辑一个要素", "警告", "不再提醒") == true)
                    {
                        Config.Instance.HideEditWarn = true;
                    }
                }
            }
            editingFeature = Mapview.Selection.SelectedFeatures.First();
            editingFeature.FeatureTable.DeleteFeatureAsync(editingFeature);

            Config.Instance.DefaultStyle.CopyStyleFrom(StyleCollection.Instance.Styles.FirstOrDefault(p => p.Table.FeatureLayer == editingFeature.FeatureTable.FeatureLayer));
            IsEditing = true;
            Mapview.SketchEditor.StartAsync(GeometryEngine.Project(editingFeature.Geometry, SpatialReferences.WebMercator));
        }

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
                catch(Exception ex)
                {
                    SnakeBar.ShowException(ex);
                }
            }
            foreach (var table in tables)
            {
                 StyleCollection.Instance.Styles.First(p => p.Table == table).UpdateFeatureCount();
            }
            Mapview.Selection.SelectedFeatures.Clear();
        }

        public async Task StopEditing()
        {
            IsEditing = false;
            await Mapview.Drawing.StopDraw();
       await     Mapview.Selection.StopSelect(false);
        }

        public async Task ResetEditingFeature()
        {
            IsEditing = false;
            Mapview.SketchEditor.Stop();
            await editingFeature.FeatureTable.AddFeatureAsync(editingFeature);
            StyleCollection.Instance.Current.UpdateFeatureCount();
        }

        private bool isEditing = false;
        public bool IsEditing
        {
            get => isEditing;
            set
            {
                if(isEditing!=value)
                {
                    isEditing = value;
                    EditingStatusChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        public event EventHandler EditingStatusChanged;
    }
}
