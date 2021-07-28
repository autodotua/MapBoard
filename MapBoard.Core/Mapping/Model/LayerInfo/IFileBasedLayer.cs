using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 表示基于文件进行数据存储的图层接口
    /// </summary>
    public interface IFileBasedLayer : IMapLayerInfo
    {
        public string[] GetFilePaths();

        public string GetMainFilePath();

        public Task SaveTo(string directory);
    }
}