using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CoCManagerBot.Helpers
{
    internal static class ApiService
    {
        internal static string CoCToken = RegHelper.GetRegValue("CoCAPIKey");
        public static async Task<string> GetJsonAsync(string url)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CoCToken);

            var result = await client.GetAsync(url);
            return await result.Content.ReadAsStringAsync();
        }

        public static async Task<T> Get<T>(string url, string parameter) where T : new()
        {
            string jsonData = string.Empty;
            url = String.Format(url, parameter);
            url = url.Replace("#", "%23");

            var returnValue = new T();

            try
            {
                jsonData = await GetJsonAsync(url);
                returnValue = JsonConvert.DeserializeObject<T>(jsonData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + $"\n{parameter}\n\n");
            }

            return returnValue;
        }
    }
}
