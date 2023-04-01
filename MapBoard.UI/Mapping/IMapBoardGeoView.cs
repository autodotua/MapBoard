using MapBoard.Mapping.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Mapping
{
    /// <summary>
    /// 地图画板的2D或3D地图
    /// </summary>
    public interface IMapBoardGeoView
    {
        /// <summary>
        /// 覆盖层帮助类
        /// </summary>
        public OverlayHelper Overlay { get; }

        /// <summary>
        /// 图层集合
        /// </summary>
        public MapLayerCollection Layers { get; }
    }
}