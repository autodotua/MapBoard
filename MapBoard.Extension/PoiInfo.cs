namespace MapBoard.Extension
{
    public class PoiInfo
    {
        /// <summary>
        /// 地名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 省份名
        /// </summary>
        public string Province { get; set; }

        /// <summary>
        /// 城市名
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// 经度
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// 距离中心点距离
        /// </summary>
        public double? Distance { get; set; }

        /// <summary>
        /// 类型描述
        /// </summary>
        public string Type { get; set; }
    }
}