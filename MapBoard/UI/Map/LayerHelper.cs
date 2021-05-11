﻿using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Basic;
using FzLib.UI.Dialog;
using MapBoard.Common;
using MapBoard.Main.Model;
using MapBoard.Main.Util;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LayerCollection = MapBoard.Main.Model.LayerCollection;

namespace MapBoard.Main.UI.Map
{
    public class LayerHelper
    {
        public ArcMapView Mapview => ArcMapView.Instance;

        public async Task<bool> AddLayerAsync(LayerInfo layer)
        {
            try
            {
                if (layer.Table == null)
                {
                    layer.Table = new ShapefileFeatureTable(layer.FileName);
                    await layer.Table.LoadAsync();
                }
                FeatureLayer fl = new FeatureLayer(layer.Table);
                Mapview.Map.OperationalLayers.Add(fl);
                layer.ApplyStyle();
                await layer.LayerComplete();
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    layer.Table.Close();
                    if (layer.Layer != null)
                    {
                        Mapview.Map.OperationalLayers.Remove(layer.Layer);
                    }
                }
                catch
                {
                }
                string error = (string.IsNullOrWhiteSpace(layer.Name) ? "样式" : "样式" + layer.Name) + "加载失败";
                await CommonDialog.ShowErrorDialogAsync(ex, error);
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
                }
            }
            catch (Exception ex)
            {
                if (SnakeBar.DefaultOwner.Owner == null)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, $"无法加载样式{style.Name}");
                }
                else
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, $"无法加载样式{style.Name}");
                }
            }
        }
    }
}