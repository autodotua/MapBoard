using FzLib;
using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MapBoard.Mapping.Model;
using System.Windows.Input;
using System.Collections;
using System.Collections.Generic;
using ModernWpf.Controls;
using GongSolutions.Wpf.DragDrop;
using FzLib.WPF;
using System.Threading.Tasks;
using System.Diagnostics;
using MapBoard.UI.Menu;

namespace MapBoard.UI
{
    /// <summary>
    /// 图层列表
    /// </summary>
    public partial class LayerListPanel : UserControlBase, IDropTarget
    {
        private bool changingSelection = false;

        /// <summary>
        /// 是否正在修改分组可见
        /// </summary>
        private bool isChangingGroupVisible = false;

        /// <summary>
        /// 帮助类
        /// </summary>
        private LayerListPanelHelper layerListHelper;

        /// <summary>
        /// 图层列表的视图类型
        /// </summary>
        private int viewType = 0;

        public LayerListPanel()
        {
            InitializeComponent();
            SetListDataTemplate();
            Config.Instance.PropertyChanged += Config_PropertyChanged;
        }

        /// <summary>
        /// 分组
        /// </summary>
        public ObservableCollection<GroupInfo> Groups { get; } = new ObservableCollection<GroupInfo>();

        /// <summary>
        /// 图层
        /// </summary>
        public MapLayerCollection Layers => MapView.Layers;

        /// <summary>
        /// 地图
        /// </summary>
        public MainMapView MapView { get; set; }

        /// <summary>
        /// 图层视图类型
        /// </summary>
        public int ViewType
        {
            get => viewType;
            set
            {
                viewType = value;
                Config.Instance.LastLayerListGroupType = value;
                dataGrid.GroupStyle.Clear();
                UpdateView();
            }
        }

        /// <summary>
        /// 拖放以改变图层顺序，在拖放过程中触发
        /// </summary>
        /// <param name="dropInfo"></param>
        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            if (MapView.CurrentTask != BoardTask.Ready //需要在没有选择或绘制时
                || ViewType != 0                                          //试图类型为顺序
                || dropInfo.TargetItem == dropInfo.Data    //拖放后顺序发生改变
             )                               //仅拖放单个图层
            {
                return;
            }
            var item = (dropInfo.Data is IList ? (dropInfo.Data as IList)[0] : dropInfo.Data) as IMapLayerInfo;

            dropInfo.Effects = DragDropEffects.Move;
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        }

        /// <summary>
        /// 拖放以改变图层顺序，在鼠标释放中触发
        /// </summary>
        /// <param name="dropInfo"></param>
        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            List<int> oldIndexs = dropInfo.Data is IMapLayerInfo m ?
                 new List<int>() { Layers.IndexOf(m) }
                 : (dropInfo.Data as IList).Cast<IMapLayerInfo>().Select(Layers.IndexOf).ToList();

            Layers.Move(oldIndexs, dropInfo.InsertIndex);
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
                    GetGroupVisible(Layers.Where(p => string.IsNullOrEmpty(p.Group))), true));
            }
            foreach (var layers in Layers
                .Where(p => !string.IsNullOrEmpty(p.Group))
               .GroupBy(p => p.Group))
            {
                Groups.Add(new GroupInfo(layers.Key, GetGroupVisible(layers)));
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

            if (Config.Instance.LastLayerListGroupType is >= 0 and < 3)
            {
                ViewType = Config.Instance.LastLayerListGroupType;
            }

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
            layerListHelper = new LayerListPanelHelper(dataGrid, this.GetWindow() as MainWindow, mapView);
        }

        /// <summary>
        /// 单击分组可见多选框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            GroupInfo group = (sender as FrameworkElement).DataContext as GroupInfo;
            isChangingGroupVisible = true;
            GetLayersByGroup(group).ForEach(p => p.LayerVisible = group.Visible.Value);
            isChangingGroupVisible = false;
        }

        /// <summary>
        /// 视图简洁模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Config_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Config.Instance.UseCompactLayerList))
            {
                SetListDataTemplate();
            }
        }

        /// <summary>
        /// 获取分组可见情况
        /// </summary>
        /// <param name="layers"></param>
        /// <returns>true为可见，false为不可见，null为部分可见</returns>
        private bool? GetGroupVisible(IEnumerable<ILayerInfo> layers)
        {
            int count = layers.Count();
            int visibleCount = layers.Where(p => p.LayerVisible).Count();
            if (visibleCount == 0)
            {
                return false;
            }
            if (count == visibleCount)
            {
                return true;
            }
            return null;
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
        /// 加载完成后，增加尺寸响应，更新视图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayerListPanel_Loaded(object sender, RoutedEventArgs e)
        {
            this.GetWindow().SizeChanged += (s, e) => UpdateLayout(e.NewSize.Height);
            UpdateLayout(this.GetWindow().ActualHeight);
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
            if (e.PropertyName == nameof(LayerInfo.LayerVisible) && !isChangingGroupVisible)
            {
                foreach (var group in Groups)
                {
                    group.Visible = GetGroupVisible(GetLayersByGroup(group));
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
        }

        /// <summary>
        /// 选中的图层变化事件。图层列表选中项不使用绑定。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedLayer_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (MapView.CurrentTask != BoardTask.Ready)
            {
                dataGrid.SelectionChanged -= SelectedLayer_Changed;

                dataGrid.SelectedItem = e.RemovedItems[0];

                dataGrid.SelectionChanged += SelectedLayer_Changed;
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
        /// 设置列表的模板，普通或简洁模式
        /// </summary>
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

        /// <summary>
        /// 根据视图高度，更新视图布局
        /// </summary>
        /// <param name="height"></param>
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

        /// <summary>
        /// 更新视图
        /// </summary>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void UpdateView()
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(dataGrid.ItemsSource) ?? throw new Exception();
            view.GroupDescriptions.Clear();
            switch (ViewType)
            {
                case 0://顺序
                    break;

                case 1://组别
                    dataGrid.GroupStyle.Add(new GroupStyle() { ContainerStyle = FindResource("groupStyle") as Style });
                    view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LayerInfo.Group)));
                    break;

                case 2://类型
                    dataGrid.GroupStyle.Add(new GroupStyle() { ContainerStyle = FindResource("groupStyle") as Style });
                    view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(MapLayerInfo.GeometryType)));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}