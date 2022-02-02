using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MapBoard.Util
{
    public class FTPHelper : IDisposable
    {
        /// <summary>
        /// FTP请求对象
        /// </summary>
        private FtpWebRequest request = null;

        /// <summary>
        /// FTP响应对象
        /// </summary>
        private FtpWebResponse response = null;

        /// <summary>
        /// FTP服务器地址
        /// </summary>
        public string ftpURI { get; private set; }

        /// <summary>
        /// FTP服务器IP
        /// </summary>
        public string ftpServerIP { get; private set; }

        /// <summary>
        /// FTP服务器默认目录
        /// </summary>
        public string ftpRemotePath { get; private set; }

        /// <summary>
        /// FTP服务器登录用户名
        /// </summary>
        public string ftpUserID { get; private set; }

        /// <summary>
        /// FTP服务器登录密码
        /// </summary>
        public string ftpPassword { get; private set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="FtpServerIP">FTP连接地址</param>
        /// <param name="FtpRemotePath">指定FTP连接成功后的当前目录, 如果不指定即默认为根目录</param>
        /// <param name="FtpUserID">用户名</param>
        /// <param name="FtpPassword">密码</param>
        public FTPHelper(string ftpServerIP, string ftpRemotePath, string ftpUserID, string ftpPassword)
        {
            this.ftpServerIP = ftpServerIP;
            this.ftpRemotePath = ftpRemotePath;
            this.ftpUserID = ftpUserID;
            this.ftpPassword = ftpPassword;
            this.ftpURI = "ftp://" + ftpServerIP + "/" + ftpRemotePath + "/";
        }

        private bool disposed = false;

        ~FTPHelper()
        {
            if (!disposed)
            {
                Dispose();
            }
        }

        /// <summary>
        /// 建立FTP链接,返回响应对象
        /// </summary>
        /// <param name="uri">FTP地址</param>
        /// <param name="ftpMethod">操作命令</param>
        /// <returns></returns>
        private async Task<FtpWebResponse> OpenAsync(Uri uri, string ftpMethod)
        {
            request = (FtpWebRequest)FtpWebRequest.Create(uri);
            request.Method = ftpMethod;
            request.UseBinary = true;
            request.KeepAlive = false;
            request.Credentials = new NetworkCredential(this.ftpUserID, this.ftpPassword);
            return (await request.GetResponseAsync()) as FtpWebResponse;
        }

        /// <summary>
        /// 建立FTP链接,返回请求对象
        /// </summary>
        /// <param name="uri">FTP地址</param>
        /// <param name="ftpMethod">操作命令</param>
        private FtpWebRequest OpenRequest(Uri uri, string ftpMethod)
        {
            request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = ftpMethod;
            request.UseBinary = true;
            request.KeepAlive = false;
            request.Credentials = new NetworkCredential(this.ftpUserID, this.ftpPassword);
            return request;
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="remoteDirectoryName">目录名</param>
        public async Task CreateDirectoryAsync(string remoteDirectoryName)
        {
            response = await OpenAsync(new Uri(ftpURI + remoteDirectoryName), WebRequestMethods.Ftp.MakeDirectory);
        }

        /// <summary>
        /// 更改目录或文件名
        /// </summary>
        /// <param name="currentName">当前名称</param>
        /// <param name="newName">修改后新名称</param>
        public void Rename(string currentName, string newName)
        {
            request = OpenRequest(new Uri(ftpURI + currentName), WebRequestMethods.Ftp.Rename);
            request.RenameTo = newName;
            response = (FtpWebResponse)request.GetResponse();
        }

        /// <summary>
        /// 切换当前目录
        /// </summary>
        /// <param name="isRoot">true:绝对路径 false:相对路径</param>
        public void GotoDirectory(string dir, bool isRoot)
        {
            if (isRoot)
                ftpRemotePath = dir;
            else
                ftpRemotePath += "/" + dir;

            ftpURI = "ftp://" + ftpServerIP + "/" + ftpRemotePath + "/";
        }

        /// <summary>
        /// 删除目录(包括下面所有子目录和子文件)
        /// </summary>
        /// <param name="remoteDirectoryName">要删除的带路径目录名：如web/test</param>
        public async Task RemoveDirectoryAsync(string remoteDirectoryName)
        {
            GotoDirectory(remoteDirectoryName, true);
            var listAll = await ListFilesAndDirectoriesAsync();
            foreach (var m in listAll)
            {
                if (m.IsDirectory)
                    await RemoveDirectoryAsync(m.Path);
                else
                    await DeleteFileAsync(m.Name);
            }
            GotoDirectory(remoteDirectoryName, true);
            response = await OpenAsync(new Uri(ftpURI), WebRequestMethods.Ftp.RemoveDirectory);
        }

        /// <summary>
        /// 文件上传
        /// </summary>
        /// <param name="localFilePath">本地文件路径</param>
        public async Task UploadAsync(string localFilePath)
        {
            FileInfo fileInf = new FileInfo(localFilePath);
            request = OpenRequest(new Uri(ftpURI + fileInf.Name), WebRequestMethods.Ftp.UploadFile);
            request.ContentLength = fileInf.Length;
            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            int contentLen;
            using var fs = fileInf.OpenRead();
            using var strm = await request.GetRequestStreamAsync();
            contentLen = await fs.ReadAsync(buff, 0, buffLength);
            while (contentLen != 0)
            {
                await strm.WriteAsync(buff, 0, contentLen);
                contentLen = await fs.ReadAsync(buff, 0, buffLength);
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="remoteFileName">要删除的文件名</param>
        public async Task DeleteFileAsync(string remoteFileName)
        {
            response = await OpenAsync(new Uri(ftpURI + remoteFileName), WebRequestMethods.Ftp.DeleteFile);
        }

        /// <summary>
        /// 获取当前目录的文件和一级子目录信息
        /// </summary>
        /// <returns></returns>
        public async Task<List<FTPFileStruct>> ListFilesAndDirectoriesAsync()
        {
            var fileList = new List<FTPFileStruct>();
            response = await OpenAsync(new Uri(ftpURI), WebRequestMethods.Ftp.ListDirectoryDetails);
            using (var stream = response.GetResponseStream())
            {
                using (var sr = new StreamReader(stream))
                {
                    string line = null;
                    while ((line = await sr.ReadLineAsync()) != null)
                    {
                        line = line.Trim();
                        //line的格式如下：

                        //drwx------   3 user group            0 Jan 22 09:49 BaseShapeFile

                        //08-18-13  11:05PM       <DIR>          aspnet_client
                        //09-22-13  11:39PM                 2946 Default.aspx
                        if (line.StartsWith('d') || line.StartsWith('-'))
                        {
                            string pattern =
    @"^([\w-]+)\s+(\d+)\s+(\w+)\s+(\w+)\s+(\d+)\s+" +
    @"(\w+\s+\d+\s+\d+|\w+\s+\d+\s+\d+:\d+)\s+(.+)$";
                            Regex regex = new Regex(pattern);
                            Match match = regex.Match(line);
                            string[] hourMinFormats =
    new[] { "MMM dd HH:mm", "MMM dd H:mm", "MMM d HH:mm", "MMM d H:mm" };
                            string[] yearFormats =
                                new[] { "MMM dd yyyy", "MMM d yyyy" };
                            string permissions = match.Groups[1].Value;
                            int inode = int.Parse(match.Groups[2].Value, CultureInfo.CurrentCulture);
                            string owner = match.Groups[3].Value;
                            string group = match.Groups[4].Value;
                            long size = long.Parse(match.Groups[5].Value, CultureInfo.CurrentCulture);
                            string s = Regex.Replace(match.Groups[6].Value, @"\s+", " ");

                            string[] formats = (s.IndexOf(':') >= 0) ? hourMinFormats : yearFormats;
                            var modified = DateTime.ParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
                            string name = match.Groups[7].Value;
                            var model = new FTPFileStruct()
                            {
                                IsDirectory = permissions[0] == 'd',
                                CreateTime = modified,
                                Name = name,
                                Path = ftpRemotePath + "/" + name
                            };
                            fileList.Add(model);
                        }
                        else
                        {
                            DateTime dtDate = DateTime.ParseExact(line.Substring(0, 8), "MM-dd-yy", null);
                            DateTime dtDateTime = DateTime.Parse(dtDate.ToString("yyyy-MM-dd") + line.Substring(8, 9));
                            string[] arrs = line.Split(' ');
                            var model = new FTPFileStruct()
                            {
                                IsDirectory = line.IndexOf("<DIR>") > 0 ? true : false,
                                CreateTime = dtDateTime,
                                Name = arrs[arrs.Length - 1],
                                Path = ftpRemotePath + "/" + arrs[arrs.Length - 1]
                            };
                            fileList.Add(model);
                        }
                    }
                }
            }
            return fileList;
        }

        /// <summary>
        /// 列出当前目录的所有文件
        /// </summary>
        public async Task<List<FTPFileStruct>> ListFilesAsync()
        {
            var listAll = await ListFilesAndDirectoriesAsync();
            var listFile = listAll.Where(m => m.IsDirectory == false).ToList();
            return listFile;
        }

        /// <summary>
        /// 列出当前目录的所有一级子目录
        /// </summary>
        public async Task<List<FTPFileStruct>> ListDirectoriesAsync()
        {
            var listAll = await ListFilesAndDirectoriesAsync();
            var listFile = listAll.Where(m => m.IsDirectory == true).ToList();
            return listFile;
        }

        /// <summary>
        /// 判断当前目录下指定的子目录或文件是否存在
        /// </summary>
        /// <param name="remoteName">指定的目录或文件名</param>
        public async Task<bool> IsExist(string remoteName)
        {
            var list = await ListFilesAndDirectoriesAsync();
            if (list.Count(m => m.Name == remoteName) > 0)
                return true;
            return false;
        }

        /// <summary>
        /// 判断当前目录下指定的一级子目录是否存在
        /// </summary>
        /// <param name="RemoteDirectoryName">指定的目录名</param>
        public async Task<bool> IsDirectoryExistAsync(string remoteDirectoryName)
        {
            var listDir = await ListDirectoriesAsync();
            if (listDir.Count(m => m.Name == remoteDirectoryName) > 0)
                return true;
            return false;
        }

        /// <summary>
        /// 判断当前目录下指定的子文件是否存在
        /// </summary>
        /// <param name="RemoteFileName">远程文件名</param>
        public async Task<bool> IsFileExistAsync(string remoteFileName)
        {
            var listFile = await ListFilesAsync();
            if (listFile.Count(m => m.Name == remoteFileName) > 0)
                return true;
            return false;
        }

        /// <summary>
        /// 下载
        /// </summary>
        /// <param name="saveFilePath">下载后的保存路径</param>
        /// <param name="downloadFileName">要下载的文件名</param>
        public async Task DownloadAsync(string saveFilePath, string downloadFileName)
        {
            using (FileStream outputStream = new FileStream(saveFilePath + "\\" + downloadFileName, FileMode.Create))
            {
                response = await OpenAsync(new Uri(ftpURI + downloadFileName), WebRequestMethods.Ftp.DownloadFile);
                using (Stream ftpStream = response.GetResponseStream())
                {
                    long cl = response.ContentLength;
                    int bufferSize = 2048;
                    int readCount;
                    byte[] buffer = new byte[bufferSize];
                    readCount = await ftpStream.ReadAsync(buffer, 0, bufferSize);
                    while (readCount > 0)
                    {
                        await outputStream.WriteAsync(buffer, 0, readCount);
                        readCount = await ftpStream.ReadAsync(buffer, 0, bufferSize);
                    }
                }
            }
        }

        public void Dispose()
        {
            disposed = true;
            if (response != null)
            {
                response.Close();
                response = null;
            }
            if (request != null)
            {
                request.Abort();
                request = null;
            }
        }
    }
}