using FzLib;
using FzLib.WPF.Dialog;

using MapBoard.IO;
using MapBoard.Model;
using MapBoard.Mapping;
using MapBoard.Util;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Drawing.Color;
using Path = System.IO.Path;
using MapBoard.Mapping.Model;
using System.Windows.Input;
using System.Collections;
using System.Collections.Generic;
using ModernWpf.Controls;
using MapBoard.UI.Component;
using GongSolutions.Wpf.DragDrop;
using System.Diagnostics;

namespace MapBoard.UI
{
    /// <summary>
    /// RendererSettingPanel.xaml 的交互逻辑
    /// </summary>
    public partial class LayerListPanel : UserControlBase, IDropTarget
    {
        private LayerListPanelHelper layerListHelper;

        public LayerListPanel()
        {
            InitializeComponent();
            SetListDataTemplate();
            Config.Instance.PropertyChanged += Instance_PropertyChanged;
        }

        private void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Config.Instance.UseCompactLayerList))
            {
                SetListDataTemplate();
            }
        }

        private void SetListDataTemplate()
        {
            if (Config.Instance.UseCompactLayerList)
            {
                dataGrid.ItemTemplate = FindResource("dtCompact") as DataTemplate;
            }
            else
            {
                dataGrid.ItemTemplate = FindResource("dtNormal") as DataTemplate;
            }
        }

        private void UpdateLayout(double height)
        {
            var r = FindResource("bdGroups") as Border;
            if (height < 800)
            {
                lvwViewTypes.HorizontalAlignment = HorizontalAlignment.Left;
                btnGroups.Visibility = Visibility.Visible;
                groupContent.Visibility = Visibility.Collapsed;
                groupContent.Content = null;
                flyoutGroups.Content = r;
                r.Background = Brushes.Transparent;
            }
            else
            {
                lvwViewTypes.HorizontalAlignment = HorizontalAlignment.Center;
                btnGroups.Visibility = Visibility.Collapsed;
                groupContent.Visibility = Visibility.Visible;
                flyoutGroups.Content = null;
                groupContent.Content = r;
                r.SetResourceReference(BackgroundProperty, "SystemControlBackgroundChromeMediumBrush");
            }
        }

        private bool changingSelection = false;

        /// <summary>
        /// 分组
        /// </summary>
        public ObservableCollection<GroupInfo> Groups { get; } = new ObservableCollection<GroupInfo>();

        public MainMapView MapView { get; set; }
        public MapLayerCollection Layers => MapView.Layers;
        private int viewType = 0;

        public int ViewType
        {
            get => viewType;
            set
            {
                viewType = value;
                this.Notify(nameof(ViewType));
                Config.Instance.LastLayerListGroupType = value;
                dataGrid.GroupStyle.Clear();

                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
                if(view==null)
                {
                    return;
                }
                view.GroupDescriptions.Clear();
                switch (value)
                {
                    case 0:
                        //dataGrid.ItemsSource = Layers;
                        break;

                    case 1:
                        dataGrid.GroupStyle.Add(new GroupStyle() { ContainerStyle = FindResource("groupStyle") as Style });

                        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LayerInfo.Group)));
                        break;

                    case 2:
                        dataGrid.GroupStyle.Add(new GroupStyle() { ContainerStyle = FindResource("groupStyle") as Style });

                        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(MapLayerInfo.GeometryType)));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="mapView"></param>
        public void Initialize(MainMapView mapView)
        {
            MapView = mapView;
            //设置图层列表的数据源并初始化选中的图层
            dataGrid.ItemsSource = mapView.Layers;
            dataGrid.SelectedItem = mapView.Layers.Selected;
            GenerateGroups();
            Layers.PropertyChanged += (p1, p2) =>
            {
                if (p2.PropertyName == nameof(MapLayerCollection.Selected) && !changingSelection)
                {
                    dataGrid.SelectedItem = mapView.Layers.Selected;
                }
            };
            Layers.CollectionChanged += Layers_CollectionChanged;
            Layers.LayerPropertyChanged += Layers_LayerPropertyChanged;
            //初始化图层列表相关操作
            layerListHelper = new LayerListPanelHelper(dataGrid, Window.GetWindow(this) as MainWindow, mapView);
        }

        /// <summary>
        /// 图层集合变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Layers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            GenerateGroups();
        }

        /// <summary>
        /// 图层集合中某一图层属性变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Layers_LayerPropertyChanged(object sender, LayerCollection.LayerPropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerInfo.Group))
            {
                GenerateGroups();
            }
            if (e.PropertyName == nameof(LayerInfo.LayerVisible) && !isChangingGroupVisiable)
            {
                foreach (var group in Groups)
                {
                    group.Visiable = GetGroupVisiable(GetLayersByGroup(group));
                }
            }
        }

        /// <summary>
        /// 图层项右键，用于显示菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListItemPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (MapView.CurrentTask != BoardTask.Ready)
            {
                return;
            }
            layerListHelper.ShowContextMenu();
        }

        /// <summary>
        /// 图层列表右键按下时，就使列表项被选中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Lvw_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            layerListHelper.RightButtonClickToSelect(e);
        }

        /// <summary>
        /// 选中的图层变化事件。图层列表选中项不使用绑定。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedLayerChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MapView.CurrentTask != BoardTask.Ready)
            {
                dataGrid.SelectionChanged -= SelectedLayerChanged;

                dataGrid.SelectedItem = e.RemovedItems[0];

                dataGrid.SelectionChanged += SelectedLayerChanged;
            }

            changingSelection = true;
            if (dataGrid.SelectedItems.Count == 1)
            {
                Layers.Selected = dataGrid.SelectedItem as MapLayerInfo;
            }
            else
            {
                Layers.Selected = null;
            }
            if (Layers.Selected != null)
            {
                (sender as System.Windows.Controls.ListView).ScrollIntoView(Layers.Selected);
            }
            changingSelection = false;
        }

        /// <summary>
        /// 传递SelectionChanged事件
        /// </summary>
        public event SelectionChangedEventHandler SelectionChanged
        {
            add
            {
                dataGrid.SelectionChanged += value;
            }
            remove
            {
                dataGrid.SelectionChanged -= value;
            }
        }

        /// <summary>
        /// 根据分组获取该分组的所有图层
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private IEnumerable<ILayerInfo> GetLayersByGroup(GroupInfo group)
        {
            if (group.IsNull)
            {
                return Layers.Where(p => string.IsNullOrEmpty(p.Group));
            }
            else
            {
                return Layers.Where(p => p.Group == group.Name);
            }
        }

        /// <summary>
        /// 获取分组可见情况
        /// </summary>
        /// <param name="layers"></param>
        /// <returns>true为可见，false为不可见，null为部分可见</returns>
        private bool? GetGroupVisiable(IEnumerable<ILayerInfo> layers)
        {
            int count = layers.Count();
            int visiableCount = layers.Where(p => p.LayerVisible).Count();
            if (visiableCount == 0)
            {
                return false;
            }
            if (count == visiableCount)
            {
                return true;
            }
            return null;
        }

        /// <summary>
        /// 生成分组多选框
        /// </summary>
        public void GenerateGroups()
        {
            Groups.Clear();
            if (Layers.Any(p => string.IsNullOrEmpty(p.Group)))
            {
                Groups.Add(new GroupInfo("（无）",
                    GetGroupVisiable(Layers.Where(p => string.IsNullOrEmpty(p.Group))), true));
            }
            foreach (var layers in Layers
                .Where(p => !string.IsNullOrEmpty(p.Group))
               .GroupBy(p => p.Group))
            {
                Groups.Add(new GroupInfo(layers.Key, GetGroupVisiable(layers)));
            }
        }

        /// <summary>
        /// 是否正在修改分组可见
        /// </summary>
        private bool isChangingGroupVisiable = false;

        /// <summary>
        /// 单击分组可见多选框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            GroupInfo group = (sender as FrameworkElement).DataContext as GroupInfo;
            isChangingGroupVisiable = true;
            GetLayersByGroup(group).ForEach(p => p.LayerVisible = group.Visiable.Value);
            isChangingGroupVisiable = false;
        }

        /// <summary>
        /// 单击分组标签
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBox txt = FindResource("flyoutContent") as TextBox;
            var parent = txt.Parent;
            //断开父级连接
            if (parent is Grid p)
            {
                p.Children.Clear();
            }
            txt.DataContext = (sender as FrameworkElement).DataContext;
            //Flyout没有焦点，所以增加一个Grid来获取失去焦点的事件
            Grid g = new Grid();
            g.Children.Add(txt);
            Flyout f = new Flyout()
            {
                Placement = ModernWpf.Controls.Primitives.FlyoutPlacementMode.Bottom,
                Content = g,
            };
            //失去焦点就关闭Flyout
            g.LostFocus += (p1, p2) =>
            {
                f.Hide();
            };
            f.ShowAt(sender as FrameworkElement);

            txt.Focus();
            txt.SelectAll();
        }

        private void LayerListPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (Config.Instance.LastLayerListGroupType is >= 0 and < 3)
            {
                ViewType = Config.Instance.LastLayerListGroupType;
            }
            Window.GetWindow(this).SizeChanged += (s, e) => UpdateLayout(e.NewSize.Height);
            UpdateLayout(Window.GetWindow(this).ActualHeight);
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            if (MapView.CurrentTask != BoardTask.Ready || ViewType != 0)
            {
                return;
            }
            if (dropInfo.TargetItem == dropInfo.Data
                || dropInfo.Data is IList)
            {
                return;
            }
            int oldIndex = Layers.IndexOf(dropInfo.Data as IMapLayerInfo);
            if (oldIndex - dropInfo.InsertIndex is < 1 and >= -1)
            {
                return;
            }
            dropInfo.Effects = DragDropEffects.Move;
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            if (MapView.CurrentTask != BoardTask.Ready || ViewType != 0)
            {
                return;
            }
            int oldIndex = Layers.IndexOf(dropInfo.Data as IMapLayerInfo);
            if (oldIndex < 0 || oldIndex - dropInfo.InsertIndex is < 1 and >= -1)
            {
                return;
            }
            int targetIndex = dropInfo.InsertIndex > oldIndex ? dropInfo.InsertIndex - 1 : dropInfo.InsertIndex;

            Layers.Move(oldIndex, targetIndex);
        }
    }
}