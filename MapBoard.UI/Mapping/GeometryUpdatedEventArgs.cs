using Esri.ArcGISRuntime.Geometry;
using System;

namespace MapBoard.Mapping
{
    public class GeometryUpdatedEventArgs : EventArgs
    {
        public GeometryUpdatedEventArgs(Geometry geometry)
        {
            Geometry = geometry;
        }

        public Geometry Geometry { get; private set; }
    }
}