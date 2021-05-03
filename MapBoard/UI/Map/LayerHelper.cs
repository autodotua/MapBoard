using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Basic;
using FzLib.UI.Dialog;
using MapBoard.Common;
using MapBoard.Main.Layer;
using MapBoard.Main.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LayerCollection = MapBoard.Main.Layer.LayerCollection;

namespace MapBoard.Main.UI.Map
{
    public class LayerHelper
    {
        public ArcMapView Mapview => ArcMapView.Instance;

        public async Task<bool> AddLayerAsync(LayerInfo style)
        {
            try
            {
                if (style.Table == null)
                {
                    style.Table = new ShapefileFeatureTable(style.FileName);
                    await style.Table.LoadAsync();
                }
                FeatureLayer layer = new FeatureLayer(style.Table);
                Mapview.Map.OperationalLayers.Add(layer);
                LayerUtility.ApplyLayers(style);
                await style.LayerComplete();
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    style.Table.Close();
                    if (style.Layer != null)
                    {
                        Mapview.Map.OperationalLayers.Remove(style.Layer);
                    }
                }
                catch
                {
                }
                string error = (string.IsNullOrWhiteSpace(style.Name) ? "样式" : "样式" + style.Name) + "加载失败";
                TaskDialog.ShowException(ex, error);
                return false;
            }
        }

        public void RemoveLayer(LayerInfo style)
        {
            try
            {
                Mapview.Map.OperationalLayers.Remove(style.Layer);
                style.Table.Close();
            }
            catch
            {
            }
        }

        public void ClearLayers()
        {
            foreach (var layer in Mapview.Map.OperationalLayers.ToArray())
            {
                Mapview.Map.OperationalLayers.Remove(layer);
                ((layer as FeatureLayer).FeatureTable as ShapefileFeatureTable).Close();
            }
        }

        public async Task LoadLayers()
        {
            Mapview.Selection.SelectedFeatures.Clear();
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;

            if (!Directory.Exists(Config.DataPath))
            {
                Directory.CreateDirectory(Config.DataPath);
                return;
            }

            foreach (var style in LayerCollection.Instance.Layers.ToArray())
            {
                if (File.Exists(Path.Combine(Config.DataPath, style.Name + ".shp")))
                {
                    await LoadLayer(style);
                }
                else
                {
                    LayerCollection.Instance.Layers.Remove(style);
                }
            }

            HashSet<string> files = Directory.EnumerateFiles(Config.DataPath)
                .Where(p => Path.GetExtension(p) == ".shp")
                .Select(p =>
                {
                    int index = p.LastIndexOf('.');
                    if (index == -1)
                    {
                        return p;
                    }
                    return p.Remove(index, p.Length - index).RemoveStart(Config.DataPath + "\\");
                }).ToHashSet();

            foreach (var name in files)
            {
                if (!LayerCollection.Instance.Layers.Any(p => p.Name == name))
                {
                    LayerInfo style = new LayerInfo();
                    style.Name = name;
                    await LoadLayer(style);
                }
            }
        }

        public async Task LoadLayer(LayerInfo style)
        {
            try
            {
                ShapefileFeatureTable featureTable = new ShapefileFeatureTable(Config.DataPath + "\\" + style.Name + ".shp");
                await featureTable.LoadAsync();
                if (featureTable.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
                {
                    //if (!LayerCollection.Instance.Layers.Contains(style))
                    //{
                    //    LayerCollection.Instance.Layers.Add(style);
                    //}
                    //if (style.FeatureCount == 0)
                    //{
                    //    RemoveStyle(style, true);
                    //}
                    //else
                    //{
                    //FeatureLayer layer = new FeatureLayer(featureTable);
                    ////Map.OperationalLayers.Add(layer);

                    //style.Table = featureTable;
                    //style.UpdateFeatureCount();
                    //SetRenderer(style);
                    //  }
                }
            }
            catch (Exception ex)
            {
                if (SnakeBar.DefaultOwner.Owner == null)
                {
                    TaskDialog.ShowException(ex, $"无法加载样式{style.Name}");
                }
                else
                {
                    SnakeBar.ShowException(ex, $"无法加载样式{style.Name}");
                }
            }
        }
    }
}