using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using MapBoard.Main.Util;
using ModernWpf.FzExtension.CommonDialog;
using MapBoard.Main.Model.Extension;
using FzLib.Extension;
using Esri.ArcGISRuntime.Data;
using System.Diagnostics;
using MapBoard.Main.UI.Model;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class FeatureHistoryDialog : Common.DialogWindowBase
    {
        private FeatureAttributeCollection[] attributes;

        private HashSet<FeatureAttributeCollection> editedAttributes = new HashSet<FeatureAttributeCollection>();

        private FeatureHistoryDialog(Window owner, MapLayerInfo layer, ArcMapView arcMap) : base(owner)
        {
            Layer = layer;
            InitializeComponent();
            Title = "操作历史记录 - " + layer.Name;
            Layer.Histories.CollectionChanged += Histories_CollectionChanged;
            arcMap.BoardTaskChanged += ArcMap_BoardTaskChanged;
        }

        private void ArcMap_BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            IsEnabled = e.NewTask == BoardTask.Ready;
        }

        private void Histories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            scr.ScrollToEnd();
        }

        private static Dictionary<MapLayerInfo, FeatureHistoryDialog> dialogs = new Dictionary<MapLayerInfo, FeatureHistoryDialog>();

        public static FeatureHistoryDialog Get(Window owner, MapLayerInfo layer, ArcMapView arcMap)
        {
            if (dialogs.ContainsKey(layer))
            {
                return dialogs[layer];
            }
            var dialog = new FeatureHistoryDialog(owner, layer, arcMap);
            dialogs.Add(layer, dialog);
            return dialog;
        }

        public FeatureAttributeCollection[] Attributes
        {
            get => attributes;
            private set => this.SetValueAndNotify(ref attributes, value, nameof(Attributes));
        }

        public int EditedFeaturesCount => editedAttributes.Count;

        public MapLayerInfo Layer { get; }

        private void Dialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Debug.Assert(dialogs.ContainsKey(Layer));
            dialogs.Remove(Layer);
        }

        private async void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            App.Current.MainWindow.IsEnabled = false;
            FeaturesChangedEventArgs current = (sender as Button).Tag as FeaturesChangedEventArgs;

            Debug.Assert(current != null);
            int index = Layer.Histories.IndexOf(current);
            int count = Layer.Histories.Count;
            try
            {
                List<FeaturesChangedEventArgs> changes = new List<FeaturesChangedEventArgs>();
                for (int i = count - 1; i >= index; i--)
                {
                    changes.Add(Layer.Histories[i]);
                }
                foreach (var change in changes)
                {
                    if (change.AddedFeatures != null)
                    {
                        await Layer.DeleteFeaturesAsync(change.AddedFeatures, FeaturesChangedSource.Undo);
                    }
                    else if (change.DeletedFeatures != null)
                    {
                        await Layer.AddFeaturesAsync(change.DeletedFeatures, FeaturesChangedSource.Undo);
                    }
                    else if (change.UpdatedFeatures != null)
                    {
                        List<UpdatedFeature> newFeature = new List<UpdatedFeature>();
                        foreach (var feature in change.UpdatedFeatures)
                        {
                            newFeature.Add(new UpdatedFeature(feature.Feature));
                            feature.Feature.Geometry = feature.OldGeometry;
                            foreach (var attr in feature.OldAttributes.Where(p => p.Key != "FID"))
                            {
                                try
                                {
                                    feature.Feature.SetAttributeValue(attr.Key, attr.Value);
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                        }
                        await Layer.UpdateFeaturesAsync(newFeature, FeaturesChangedSource.Undo);
                    }
                }
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "撤销失败");
            }
            finally
            {
                for (int i = 0; i < count; i++)
                {
                    Layer.Histories[i].CanUndo = false;
                }
                IsEnabled = true;
                App.Current.MainWindow.IsEnabled = true;
            }
        }

        private void UndoButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            FeaturesChangedEventArgs current = (sender as Button).Tag as FeaturesChangedEventArgs;

            Debug.Assert(current != null);
            int index = Layer.Histories.IndexOf(current);
            int count = Layer.Histories.Count;
            (sender as Button).ToolTip = $"撤销{count - index}条操作";
        }
    }
}