using MapBoard.GeoShare.Core.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Services
{

    public class HttpService
    {
        public const string Url_Login = "/User/Login";

        private HttpClient httpClient;

        public HttpService()
        {
            httpClient = new HttpClient();
        }

        public Task GetAsync(string url)
        {
            return GetAsync<object>(url);
        }
        public async Task<T> GetAsync<T>(string url)
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            HttpResponseContainer<T> container = JsonConvert.DeserializeObject<HttpResponseContainer<T>>(responseContent);
            if (!container.Success)
            {
                throw new Exception($"请求失败：{container.Message}");
            }
            return container.Data;
        }

        public Task PostAsync(string url, object requestData)
        {
            return PostAsync<object>(url, requestData);
        }
        public async Task<T> PostAsync<T>(string url, object requestData)
        {
            string jsonData = JsonConvert.SerializeObject(requestData);
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            HttpResponseContainer<T> container = JsonConvert.DeserializeObject<HttpResponseContainer<T>>(responseContent);
            if (!container.Success)
            {
                throw new Exception($"请求失败：{container.Message}");
            }
            return container.Data;
        }
    }
}
