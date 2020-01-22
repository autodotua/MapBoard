using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MapBoard.TileDownloaderSplicer
{
    public static class NetHelper
    {
        public static void HttpDownload(string url, string path)
        {
            string tempPath = Path.Combine(Path.GetDirectoryName(path), "temp");
            Directory.CreateDirectory(tempPath);  //创建临时文件目录
            string tempFile = Path.Combine(tempPath, Guid.NewGuid().ToString("N")); //临时文件
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);    //存在则删除
                }

                using (FileStream fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // 设置参数
                    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                    request.Timeout = Config.Instance.RequestTimeOut;
                    request.UserAgent = Config.Instance.DownloadUserAgent;
                    //发送请求并获取相应回应数据
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                    //直到request.GetResponse()程序才开始向目标网页发送Post请求
                    using Stream responseStream = response.GetResponseStream();
                    responseStream.ReadTimeout = Config.Instance.ReadTimeOut;
                    //创建本地文件写入流
                    byte[] bArr = new byte[1024 * 1024];
                    int size = responseStream.Read(bArr, 0, bArr.Length);
                    while (size > 0)
                    {
                        fs.Write(bArr, 0, size);
                        size = responseStream.Read(bArr, 0, bArr.Length);
                    }
                    //stream.Close();
                    response.Close();
                }
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                int tryTimes = 0;
                while (File.Exists(path))
                {
                    tryTimes++;
                    if (tryTimes >= 10)
                    {
                        throw new Exception("尝试删除文件失败");
                    }
                   Thread.Sleep(100);
                }

                File.Move(tempFile, path);

            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {

            }

        }
        /// <summary>
        /// HTTP服务器
        /// </summary>
        private static TcpListener tcpListener;
        public async static Task StartServer()
        {
            IPAddress localaddress = IPAddress.Loopback;

            // 创建可以访问的断点，49155表示端口号，如果这里设置为0，表示使用一个由系统分配的空闲的端口号
            IPEndPoint endpoint = new IPEndPoint(localaddress, Config.Instance.ServerPort);
            if (tcpListener == null)
            {
                // 创建Tcp 监听器
                tcpListener = new TcpListener(endpoint);
            }
            // 启动监听
            tcpListener.Start();
            while (true)
            {
                if (tcpListener.GetType().GetRuntimeProperties().ToArray()[1].GetValue(tcpListener).Equals(false))
                {
                    return;
                }
                try
                {
                    // 等待客户连接
                    TcpClient client = await tcpListener.AcceptTcpClientAsync();
                    // 获得一个网络流对象
                    // 该网络流对象封装了Socket的输入和输出操作
                    // 此时通过对网络流对象进行写入来返回响应消息
                    // 通过对网络流对象进行读取来获得请求消息
                    Task.Run(async () =>
                   {
                       await SendPic(client);
                   });

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                // 关闭与客户端的连接
                //client.Close();
                // break;
            }
        }

        private static async Task SendPic(TcpClient client)
        {
            NetworkStream netstream = client.GetStream();
            // 把客户端的请求数据读入保存到一个数组中
            byte[] buffer = new byte[2048];

            int receivelength = netstream.Read(buffer, 0, 2048);
            string requeststring = Encoding.UTF8.GetString(buffer, 0, receivelength);

            // 在服务器端输出请求的消息
            //Debug.WriteLine(requeststring);

            Regex r = new Regex("GET /([0-9]+)-([0-9]+)-([0-9]+) HTTP");
            if (r.IsMatch(requeststring))
            {

                var match = r.Match(requeststring);
                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);
                int level = int.Parse(match.Groups[3].Value);
                string file = $"Download\\{level}\\{x}-{y}.{Config.Instance.FormatExtension}";
                byte[] responseBodyBytes = null;
                string statusLine;
                if (File.Exists(file))
                {

                    statusLine = "HTTP/1.1 200 OK\r\n";
                    responseBodyBytes = File.ReadAllBytes(file);
                }
                else
                {
                    statusLine = "HTTP/1.1 404 Not Found\r\n";
                    //responseBodyBytes = Properties.Resources.ErrorPic;
                }

                // 服务器端做出相应内容
                // 响应的状态行;
                byte[] responseStatusLineBytes = Encoding.UTF8.GetBytes(statusLine);
                //string responseBody = "<html><head><title>Default Page</title></head><body><p style='font:bold;font-size:24pt'>Welcome you</p></body></html>";
                string responseHeader =
                     string.Format(
                         "Content-Type: image/{0}; charset=UTf-8\r\nContent-Length: {1}\r\n", Config.Instance.FormatExtension, responseBodyBytes.Length);

                byte[] responseHeaderBytes = Encoding.UTF8.GetBytes(responseHeader);
                //byte[] responseBodyBytes = Encoding.UTF8.GetBytes(responseBody);

                // 写入状态行信息
                netstream.Write(responseStatusLineBytes, 0, responseStatusLineBytes.Length);
                // 写入回应的头部
                netstream.Write(responseHeaderBytes, 0, responseHeaderBytes.Length);
                // 写入回应头部和内容之间的空行
                netstream.Write(new byte[] { 13, 10 }, 0, 2);

                // 写入回应的内容
                netstream.Write(responseBodyBytes, 0, responseBodyBytes.Length);

                //}
                //else
                //{
                //    string statusLine = "HTTP/1.1 404 Not Found\r\n";
                //    byte[] responseStatusLineBytes = Encoding.UTF8.GetBytes(statusLine);
                //    netstream.Write(responseStatusLineBytes, 0, responseStatusLineBytes.Length);

                //}

                await netstream.FlushAsync();
            }
        }

        public static void StopServer()
        {

            tcpListener.Stop();
        }
    }
}
