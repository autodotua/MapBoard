﻿using MapBoard.Mapping;
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
using ModernWpf.Controls;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class DefinitionExpressionDialog : LayerDialogBase
    {
        private DefinitionExpressionDialog(Window owner, IMapLayerInfo layer, MainMapView arcMap) : base(owner, layer, arcMap)
        {
            InitializeComponent();
            Title = "筛选显示图形 - " + layer.Name;
            Expression = layer.DefinitionExpression;
            throw new NotImplementedException();
        }

        public string Expression { get; set; }
        public string DateFieldName { get; set; }

        public static DefinitionExpressionDialog Get(Window owner, IMapLayerInfo layer, MainMapView mapView)
        {
            return GetInstance(layer, () => new DefinitionExpressionDialog(owner, layer, mapView));
        }

        private void ArcMap_BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            IsEnabled = e.NewTask == BoardTask.Ready;
        }

        private void Dialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private async void DateExtentButton_Click(object sender, RoutedEventArgs e)
        {
            DateRangeDialog dialog = new DateRangeDialog();
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                Expression = $"{DateFieldName} >= date '{dialog.From:yyyy-MM-dd}' and {DateFieldName} <= date '{dialog.To:yyyy-MM-dd}'";
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            Layer.DefinitionExpression = Expression;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}