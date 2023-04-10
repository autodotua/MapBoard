using MapBoard.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using MapBoard.Util;
using ModernWpf.FzExtension.CommonDialog;
using FzLib;
using Esri.ArcGISRuntime.Data;
using System.Diagnostics;
using MapBoard.Mapping.Model;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// 要素历史对话框
    /// </summary>
    public partial class FeatureHistoryDialog : LayerDialogBase
    {
        private FeatureHistoryDialog(Window owner, IEditableLayerInfo layer, MainMapView arcMap) : base(owner, layer, arcMap)
        {
            InitializeComponent();
            Title = "操作历史记录 - " + layer.Name;
            layer.Histories.CollectionChanged += Histories_CollectionChanged;
            arcMap.BoardTaskChanged += ArcMap_BoardTaskChanged;
        }

        /// <summary>
        /// 创建或打开图层对应的<see cref="FeatureHistoryDialog"/>
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="layer"></param>
        /// <param name="mapView"></param>
        /// <returns></returns>
        public static FeatureHistoryDialog Get(Window owner, IEditableLayerInfo layer, MainMapView mapView)
        {
            return GetInstance(layer, () => new FeatureHistoryDialog(owner, layer, mapView));
        }

        /// <summary>
        /// 仅在普通状态下可编辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArcMap_BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            IsEnabled = e.NewTask == BoardTask.Ready;
        }

        /// <summary>
        /// 历史改变，自动移到最下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Histories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            scr.ScrollToEnd();
        }

        /// <summary>
        /// 单击撤销按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            Owner.IsEnabled = false;
            FeaturesChangedEventArgs current = (sender as Button).DataContext as FeaturesChangedEventArgs;
            var layer = Layer as IEditableLayerInfo;
            Debug.Assert(current != null);
            Debug.Assert(layer != null);
            int index = layer.Histories.IndexOf(current);
            int count = layer.Histories.Count;
            try
            {
                List<FeaturesChangedEventArgs> changes = new List<FeaturesChangedEventArgs>();
                for (int i = count - 1; i >= index; i--)
                {
                    changes.Add(layer.Histories[i]);
                }
                foreach (var change in changes)
                {
                    if (change.AddedFeatures != null)
                    {
                        await layer.DeleteFeaturesAsync(change.AddedFeatures, FeaturesChangedSource.Undo);
                    }
                    else if (change.DeletedFeatures != null)
                    {
                        await layer.AddFeaturesAsync(change.DeletedFeatures, FeaturesChangedSource.Undo);
                    }
                    else if (change.UpdatedFeatures != null)
                    {
                        List<UpdatedFeature> newFeature = new List<UpdatedFeature>();
                        foreach (var feature in change.UpdatedFeatures)
                        {
                            newFeature.Add(new UpdatedFeature(feature.Feature));
                            feature.Feature.Geometry = feature.OldGeometry;
                            foreach (var attr in feature.OldAttributes.Where(p => !FieldExtension.IsIdField(p.Key)))
                            {
                                try
                                {
                                    feature.Feature.SetAttributeValue(attr.Key, attr.Value);
                                }
                                catch (Exception ex)
                                {
                                    App.Log.Error("设置属性失败", ex);
                                }
                            }
                        }
                        await layer.UpdateFeaturesAsync(newFeature, FeaturesChangedSource.Undo);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Log.Error("撤销失败", ex);
                IsEnabled = true;
                await CommonDialog.ShowErrorDialogAsync(ex, "撤销失败");
            }
            finally
            {
                for (int i = 0; i < count; i++)
                {
                    layer.Histories[i].CanUndo = false;
                }
                IsEnabled = true;
                Owner.IsEnabled = true;
            }
        }

        /// <summary>
        /// 鼠标移到撤销按钮上，显示共撤销几步
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UndoButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            FeaturesChangedEventArgs current = (sender as Button).DataContext as FeaturesChangedEventArgs;

            var layer = Layer as IEditableLayerInfo;
            Debug.Assert(current != null);
            Debug.Assert(layer != null);
            int index = layer.Histories.IndexOf(current);
            int count = layer.Histories.Count;
            (sender as Button).ToolTip = $"撤销{count - index}条操作";
        }
    }
}