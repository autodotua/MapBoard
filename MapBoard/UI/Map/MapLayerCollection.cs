using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Main.Model;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MapBoard.Main.UI.Map
{
    public class MapLayerCollection : LayerCollection
    {
        public const string LayersFileName = "layers.json";
        private static MapLayerCollection instance;

        private LayerInfo selected;

        private MapLayerCollection()
        {
        }

        public static event EventHandler LayerInstanceChanged;

        public static MapLayerCollection Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new Exception("请先调用LoadInstanceAsync方法初始化实例");
                }
                return instance;
            }
        }

        public static bool IsInstanceLoaded { get; private set; } = false;

        public LayerInfo Selected
        {
            get => selected;
            set => this.SetValueAndNotify(ref selected, value, nameof(Selected));
        }

        public static async Task LoadInstanceAsync()
        {
            string path = Path.Combine(Config.DataPath, LayersFileName);
            if (!File.Exists(path))
            {
                instance = new MapLayerCollection();
                return;
            }
            instance = FromFile<MapLayerCollection>(path, () => new MapLayerCollection());
            foreach (var layer in instance.layers)
            {
                await instance.AddAsync(layer, false);
            }

            if (instance.SelectedIndex >= 0
                && instance.SelectedIndex < instance.Count)
            {
                instance.Selected = instance[instance.SelectedIndex];
            }
            LayerInstanceChanged?.Invoke(Instance, new EventArgs());
            IsInstanceLoaded = true;
        }

        public async static Task ResetLayersAsync()
        {
            instance.Clear();
            await LoadInstanceAsync();
        }

        public Task<bool> AddAsync(LayerInfo layer)
        {
            return AddAsync(layer, true);
        }

        public void Clear()
        {
            ArcMapView.Instance.Layer.ClearLayers();
            layers.Clear();
        }

        public async Task<bool> InsertAsync(int index, LayerInfo layer)
        {
            if (await ArcMapView.Instance.Layer.AddLayerAsync(layer))
            {
                layer.PropertyChanged += LayerPropertyChanged;
                layers.Insert(index, layer);
                return true;
            }
            return false;
        }

        public void Move(int fromIndex, int toIndex)
        {
            ArcMapView.Instance.Map.OperationalLayers.Move(fromIndex, toIndex);
            layers.Move(fromIndex, toIndex);
        }

        public void Remove(LayerInfo layer)
        {
            ArcMapView.Instance.Layer.RemoveLayer(layer);
            layer.PropertyChanged -= LayerPropertyChanged;
            layers.Remove(layer);
        }

        private async Task<bool> AddAsync(LayerInfo layer, bool addToCollection)
        {
            if (await ArcMapView.Instance.Layer.AddLayerAsync(layer))
            {
                layer.PropertyChanged += LayerPropertyChanged;
                if (addToCollection)
                {
                    layers.Add(layer);
                }
                return true;
            }
            return false;
        }

        public void Save()
        {
            Save(Path.Combine(Config.DataPath, LayersFileName));
        }
    }
}