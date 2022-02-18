using Esri.ArcGISRuntime.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace MapBoard.Mapping.Model
{
    public class FeaturesChangedEventArgs : EventArgs, INotifyPropertyChanged
    {
        public FeaturesChangedEventArgs(MapLayerInfo layer,
            IEnumerable<Feature> addedFeatures,
            IEnumerable<Feature> deletedFeatures,
            IEnumerable<UpdatedFeature> changedFeatures,
            FeaturesChangedSource source)
        {
            Source = source;
            Time = DateTime.Now;
            int count = 0;
            if (deletedFeatures != null)
            {
                count++;
                DeletedFeatures = new List<Feature>(deletedFeatures).AsReadOnly();
            }
            if (addedFeatures != null)
            {
                count++;
                AddedFeatures = new List<Feature>(addedFeatures).AsReadOnly();
            }
            if (changedFeatures != null)
            {
                count++;
                UpdatedFeatures = new List<UpdatedFeature>(changedFeatures).AsReadOnly();
            }
            Debug.Assert(count == 1);
            Layer = layer;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public IReadOnlyList<Feature> AddedFeatures { get; }

        public bool CanUndo { get; set; } = true;

        public IReadOnlyList<Feature> DeletedFeatures { get; }
        public MapLayerInfo Layer { get; }
        public FeaturesChangedSource Source { get; }
        public DateTime Time { get; }
        public IReadOnlyList<UpdatedFeature> UpdatedFeatures { get; }
    }
}