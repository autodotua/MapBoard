using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Control.Dialog;
using MapBoard.Style;
using MapBoard.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MapBoard.UI.Map
{
    public class EditHelper
    {

        public ArcMapView Mapview => ArcMapView.Instance;

        /// <summary>
        /// 编辑
        /// </summary>
        public async void StartEdit(EditMode mode)
        {
            //if (Mapview.Selection.SelectedFeatures.Count > 1)
            //{
            //    if (!Config.Instance.HideEditWarn)
            //    {
            //        if (TaskDialog.ShowWithCheckBox(App.Current.MainWindow, "编辑时，只能同时编辑一个要素", "警告", "不再提醒") == true)
            //        {
            //            Config.Instance.HideEditWarn = true;
            //        }
            //    }
            //}
            Mode = mode;

            editingFeature = Mapview.Selection.SelectedFeatures.First();
            Mapview.Drawing.Label = editingFeature.Attributes[Resource.Resource.DisplayFieldName] as string;
            if (editingFeature.Attributes[Resource.Resource.TimeExtentFieldName] is DateTimeOffset date)
            {
                Mapview.Drawing.Date = date;
            }
            else
            {
                Mapview.Drawing.Date = null;
            }
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Edit;
            // Config.Instance.DefaultStyle.CopyStyleFrom(StyleCollection.Instance.Styles.FirstOrDefault(p => p.Table.FeatureLayer == editingFeature.FeatureTable.FeatureLayer));
            if (mode == EditMode.Draw)
            {
                await editingFeature.FeatureTable.DeleteFeatureAsync(editingFeature);
                //ShowBar("正在编辑");
                await Mapview.SketchEditor.StartAsync(GeometryEngine.Project(editingFeature.Geometry, SpatialReferences.WebMercator));
            }
            else if (mode == EditMode.Cut)
            {
                //ShowBar("请画一条直线用于切割");
                await Mapview.SketchEditor.StartAsync(Esri.ArcGISRuntime.UI.SketchCreationMode.Polyline);

            }
        }

        //private void ShowBar(string message)
        //{
        //    SnakeBar bar = new SnakeBar(Application.Current.MainWindow);
        //    lastBar = bar;
        //    bar.ButtonClick += async (p1, p2) =>
        //    {
        //        await StopEditing();
        //    };
        //    bar.ShowButton = true;
        //    bar.Duration = Duration.Forever;
        //    bar.ButtonContent = "完成";
        //    bar.ShowMessage(message);
        //}

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
                    StyleCollection.Instance.Selected.UpdateFeatureCount();
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
                StyleCollection.Instance.Styles.First(p => p.Table == table).UpdateFeatureCount();
            }
            Mapview.Selection.SelectedFeatures.Clear();
        }

        public async Task StopEditing()
        {
            //if (lastBar != null)
            //{
            //    lastBar.Hide();
            //    lastBar = null;
            //}
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
                StyleCollection.Instance.Selected.UpdateFeatureCount();
            }

            //if (lastBar != null)
            //{
            //    lastBar.Hide();
            //    lastBar = null;
            //}
            Mapview.Selection.ClearSelection();
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;
        }

        //private bool isEditing = false;
        //public bool IsEditing
        //{
        //    get => isEditing;
        //    set
        //    {
        //        if (isEditing != value)
        //        {
        //            isEditing = value;
        //            EditingStatusChanged?.Invoke(this, new EventArgs());
        //        }
        //    }
        //}

        //public event EventHandler EditingStatusChanged;
    }
}
