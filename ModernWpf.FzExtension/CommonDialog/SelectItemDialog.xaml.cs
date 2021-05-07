using FzLib.Extension;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ModernWpf.FzExtension.CommonDialog
{
    public partial class SelectItemDialog : CommonDialog
    {
        internal SelectItemDialog()
        {
            InitializeComponent();
        }

        private ObservableCollection<DialogItem> items;

        public ObservableCollection<DialogItem> Items
        {
            get => items;
            set => this.SetValueAndNotify(ref items, value, nameof(Items));
        }

        public int SelectedIndex { get; private set; } = -1;

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedIndex = (sender as ListView).SelectedIndex;
            var item = (sender as ListView).SelectedItem as DialogItem;

            Hide();
            item.SelectAction?.Invoke();
        }
    }

    public class DialogItem : INotifyPropertyChanged
    {
        private string title;

        public string Title
        {
            get => title;
            set => this.SetValueAndNotify(ref title, value, nameof(Title));
        }

        private string detail;

        public string Detail
        {
            get => detail;
            set => this.SetValueAndNotify(ref detail, value, nameof(Detail));
        }

        private Action selectAction;

        public DialogItem(string title, string detail = null, Action selectAction = null)
        {
            Title = title;
            Detail = detail;
            SelectAction = selectAction;
        }

        public DialogItem()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Action SelectAction
        {
            get => selectAction;
            set => this.SetValueAndNotify(ref selectAction, value, nameof(SelectAction));
        }
    }
}