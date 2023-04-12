namespace MapBoard.UI.Model
{
    /// <summary>
    /// 下载状态
    /// </summary>
    public enum DownloadStatus
    {
        /// <summary>
        /// 正在下载
        /// </summary>
        Downloading,

        /// <summary>
        /// 已经暂停
        /// </summary>
        Paused,

        /// <summary>
        /// 已经结束
        /// </summary>
        Stop,

        /// <summary>
        /// 正在暂停中（已经点击暂停按钮，通知任务暂停）
        /// </summary>
        Pausing
    }
}