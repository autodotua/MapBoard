using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 表示基于文件进行数据存储的图层接口
    /// </summary>
    public interface IFileBasedLayer : IMapLayerInfo
    {
        /// <summary>
        /// 获取所有所属文件
        /// </summary>
        /// <returns></returns>
        public string[] GetFilePaths();

        /// <summary>
        /// 获取图层所属的文件集合中的主文件
        /// </summary>
        /// <returns></returns>
        public string GetMainFilePath();

        /// <summary>
        /// 保存到目标目录
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public Task SaveTo(string directory);
    }
}