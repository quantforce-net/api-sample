using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace common.Helpers
{
    public class Rest
    {
        public Rest()
        {
        }

        public Rest(string token)
        {
            Headers.Add("token", token);
        }

        public HttpClient Client()
        {
            var result = new HttpClient();
            foreach (var h in Headers)
                result.DefaultRequestHeaders.Add(h.Key, h.Value);
            return result;
        }

        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string content = null)
        {
            var request = new HttpRequestMessage(method, url);
            if (content != null)
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
            var response = await Client().SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new Exception("Request failed:" + await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            return response;
        }

        public async Task<T> GetAsync<T>(string uri)
        {
            var response = await SendAsync(HttpMethod.Get, uri);
            response.EnsureSuccessStatusCode();
            string s = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(s);
        }

        public async Task<T> PostAsync<T>(string uri, object data = null)
        {
            var response = await SendAsync(HttpMethod.Post, uri, JsonConvert.SerializeObject(data));
            response.EnsureSuccessStatusCode();
            var s = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(s);
        }

        public async Task<T> PostFileAsync<T>(string uri, byte[] data, int offset, int length)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new System.Net.Http.ByteArrayContent(data, offset, length);
            var response = await Client().SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new Exception("Request failed:" + await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            response.EnsureSuccessStatusCode();
            var s = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(s);
        }

        public async Task<T> PutAsync<T>(string uri, object data = null)
        {
            var response = await SendAsync(HttpMethod.Put, uri, JsonConvert.SerializeObject(data));
            response.EnsureSuccessStatusCode();
            var s = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(s);
        }
    }
}
