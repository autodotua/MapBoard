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
        public const string Url_Register = "/User/Register";
        public const string Url_LatestLocations = "/Loc/Latest";
        public const string Url_ReportLocation = "/Loc/New";

        private HttpClient httpClient;

        public HttpService()
        {
            httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        public Task GetAsync(string url)
        {
            return GetAsync<object>(url);
        }
        public async Task<T> GetAsync<T>(string url)
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);
            string responseContent = null;
            try
            {
                try
                {
                    responseContent = await response.Content.ReadAsStringAsync();
                }
                catch
                {

                }
                response.EnsureSuccessStatusCode();
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            catch (HttpRequestException ex)
            {
                if (!string.IsNullOrEmpty(responseContent))
                {
                    throw new HttpRequestException(responseContent, ex, ex.StatusCode);
                }
                else
                {
                    throw;
                }
            }
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
            string responseContent = null;
            try
            {
                try
                {
                    responseContent = await response.Content.ReadAsStringAsync();
                }
                catch
                {

                }
                response.EnsureSuccessStatusCode();
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            catch(HttpRequestException ex)
            {
                if(!string.IsNullOrEmpty(responseContent))
                {
                    throw new HttpRequestException(responseContent, ex,ex.StatusCode);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
