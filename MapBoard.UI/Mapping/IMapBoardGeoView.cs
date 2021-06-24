using MapBoard.Mapping.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Mapping
{
    public interface IMapBoardGeoView
    {
        public OverlayHelper Overlay { get; }
        public MapLayerCollection Layers { get; }
    }
}