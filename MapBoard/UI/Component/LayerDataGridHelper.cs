using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace MapBoard.Main.UI.Component
{
    public class LayerListViewHelper : ItemControlHelper<ListView, ListViewItem>
    {
        public LayerListViewHelper(ListView view) : base(view)
        {
        }

        protected override IList GetSelectedItems()
        {
            return View.SelectedItems;
        }
    }

    public class LayerDataGridHelper : ItemControlHelper<DataGrid, DataGridRow>
    {
        public LayerDataGridHelper(DataGrid view) : base(view)
        {
            view.PreparingCellForEdit += CellEditBeginning;
            view.CellEditEnding += CellEditEnding;
        }

        public bool IsCellEditing { get; private set; }

        private void CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            IsCellEditing = false;
            EditingCellLocation = null;
            EditingCell = null;
        }

        private void CellEditBeginning(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            IsCellEditing = true;
            EditingCellLocation = (e.Row, e.Column);
            EditingCell = e.EditingElement;
        }

        public (DataGridRow Row, DataGridColumn Column)? EditingCellLocation { get; private set; }
        public FrameworkElement EditingCell { get; private set; }

        protected override IList GetSelectedItems()
        {
            return View.SelectedItems;
        }

        public bool IsEditing => GetEditingRow() != null;
        public override bool CanDragDrop => !IsCellEditing;

        public DataGridRow GetEditingRow()
        {
            var index = View.SelectedIndex;
            if (index >= 0)
            {
                DataGridRow selected = GetItem(index);
                if (selected.IsEditing) return selected;
            }

            for (int i = 0; i < View.Items.Count; i++)
            {
                if (i == index) continue;
                var item = GetItem(i);
                if (item.IsEditing) return item;
            }

            return null;
        }
    }

    public abstract class ItemControlHelper<TView, TViewItem> where TView : Selector where TViewItem : Control
    {
        public TView View { get; private set; }

        public ItemControlHelper(TView view)
        {
            if (!(view is MultiSelector
                || view is ListBox))
            {
                throw new Exception("不支持的View");
            }
            View = view;
        }

        public void EnableDragAndDropItem()
        {
            View.AllowDrop = true;
            View.MouseMove += SingleMouseMove;
            View.Drop += SingleDrop;
        }

        public void EnableDragAndDropItems()
        {
            View.AllowDrop = true;
            View.MouseMove += MultiMouseMove;
            View.Drop += MultiDrop;
        }

        public void DisableDragAndDropItems()
        {
            View.AllowDrop = true;
            View.MouseMove -= MultiMouseMove;
            View.Drop -= MultiDrop;
        }

        private void SingleMouseMove(object sender, MouseEventArgs e)
        {
            if (!CanDragDrop)
            {
                return;
            }
            TView listview = sender as TView;
            LayerInfo select = (LayerInfo)listview.SelectedItem;
            if (listview.SelectedIndex < 0)
            {
                return;
            }
            if (e.LeftButton == MouseButtonState.Pressed && IsMouseOverTarget(GetItem(listview.SelectedIndex), new GetPositionDelegate(e.GetPosition)))
            {
                DataObject data = new DataObject(typeof(LayerInfo), select);

                DragDrop.DoDragDrop(listview, data, DragDropEffects.Move);
            }
        }

        public virtual bool CanDragDrop => true;

        private void SingleDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(LayerInfo)))
            {
                LayerInfo item = (LayerInfo)e.Data.GetData(typeof(LayerInfo));
                //index为放置时鼠标下元素项的索引
                int index = GetCurrentIndex(new GetPositionDelegate(e.GetPosition));
                if (index > -1)
                {
                    //拖动元素集合的第一个元素索引
                    int oldIndex = (View.ItemsSource as MapLayerCollection).IndexOf(item);
                    if (oldIndex == index)
                    {
                        return;
                    }

                    (View.ItemsSource as MapLayerCollection).Move(oldIndex, index);
                    SingleItemDragDroped?.Invoke(this, new SingleItemDragDropedEventArgs(oldIndex, index));
                }
            }
        }

        private void MultiMouseMove(object sender, MouseEventArgs e)
        {
            //TView listview = sender as TView;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                IList list = GetSelectedItems();
                DataObject data = new DataObject(typeof(IList), list);
                if (list.Count > 0)
                {
                    DragDrop.DoDragDrop(View, data, DragDropEffects.Move);
                }
            }
        }

        private void MultiDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(IList)))
            {
                IList peopleList = e.Data.GetData(typeof(IList)) as IList;
                //index为放置时鼠标下元素项的索引
                int index = GetCurrentIndex(new GetPositionDelegate(e.GetPosition));
                if (index > -1)
                {
                    LayerInfo Logmess = (LayerInfo)peopleList[0];
                    //拖动元素集合的第一个元素索引
                    int OldFirstIndex = (View.ItemsSource as MapLayerCollection).IndexOf(Logmess);
                    for (int i = 0; i < peopleList.Count; i++)
                    {
                        (View.ItemsSource as MapLayerCollection).Move(OldFirstIndex, index);
                    }
                    GetSelectedItems().Clear();
                }
            }
        }

        private int GetCurrentIndex(GetPositionDelegate getPosition)
        {
            int index = -1;
            for (int i = 0; i < View.Items.Count; ++i)
            {
                TViewItem item = GetItem(i);
                if (item != null && IsMouseOverTarget(item, getPosition))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        private bool IsMouseOverTarget(Visual target, GetPositionDelegate getPosition)
        {
            if (target == null)
            {
                return false;
            }
            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = getPosition((IInputElement)target);
            return bounds.Contains(mousePos);
        }

        private delegate Point GetPositionDelegate(IInputElement element);

        public TViewItem GetItem(int index)
        {
            if (View.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                return null;
            }
            if (index < 0)
            {
                return null;
            }
            return View.ItemContainerGenerator.ContainerFromIndex(index) as TViewItem;
        }

        protected abstract IList GetSelectedItems();

        public delegate void SingleItemDragDropedEventHandler(object sender, SingleItemDragDropedEventArgs e);

        public event SingleItemDragDropedEventHandler SingleItemDragDroped;

        public class SingleItemDragDropedEventArgs : EventArgs
        {
            public SingleItemDragDropedEventArgs(int oldIndex, int newIndex)
            {
                OldIndex = oldIndex;
                NewIndex = newIndex;
            }

            public int OldIndex { get; private set; }
            public int NewIndex { get; private set; }
        }
    }
}