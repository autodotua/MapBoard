using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MapBoard.Util
{
    public class HttpServerUtil
    {
        private HttpListener listener;

        public static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public static string[] GetIPs()
        {
            var a = Dns.GetHostEntry(Dns.GetHostName());
            return a.AddressList.Select(p => p.ToString()).ToArray();
        }

        public void Stop()
        {
            listener.Stop();
        }

        public async Task Start(string ip, int port, Func<HttpListenerRequest, Task<byte[]>> getResponse)
        {
            if (getResponse == null)
            {
                throw new ArgumentNullException();
            }
            listener = new HttpListener();
            listener.Prefixes.Add($"http://{ip}:{port}/");
            listener.Start();
            var requests = new HashSet<Task>();
            for (int i = 0; i < 10; i++)
            {
                requests.Add(listener.GetContextAsync());
            }
            while (true)
            {
                Task t = await Task.WhenAny(requests);
                requests.Remove(t);

                if (t is Task<HttpListenerContext>)
                {
                    var context = (t as Task<HttpListenerContext>).Result;
                    requests.Add(ProcessRequestAsync(context, getResponse));
                    requests.Add(listener.GetContextAsync());
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context, Func<HttpListenerRequest, Task<byte[]>> getResponse)
        {
            try
            {
                var response = context.Response;
                var request = context.Request;
                byte[] buffer = await getResponse(request);

                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                await output.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                HttpListenerException?.Invoke(this, new HttpListenerExceptionEventArgs(context, ex));
            }
            finally
            {
                try
                {
                    context.Response.OutputStream.Close();
                }
                catch
                {
                }
            }
        }

        public event EventHandler<HttpListenerExceptionEventArgs> HttpListenerException;
    }

    public class HttpListenerExceptionEventArgs : EventArgs
    {
        public HttpListenerExceptionEventArgs(HttpListenerContext context, Exception exception)
        {
            Context = context;
            Exception = exception;
        }

        public HttpListenerContext Context { get; set; }
        public Exception Exception { get; set; }
    }
}