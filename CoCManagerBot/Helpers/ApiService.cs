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
        internal static string CoCToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiIsImtpZCI6IjI4YTMxOGY3LTAwMDAtYTFlYi03ZmExLTJjNzQzM2M2Y2NhNSJ9.eyJpc3MiOiJzdXBlcmNlbGwiLCJhdWQiOiJzdXBlcmNlbGw6Z2FtZWFwaSIsImp0aSI6IjA4MWNlYjE2LTViMjEtNDQ1Mi04MjcyLTA5NDNiOTcyMDBkMiIsImlhdCI6MTUwOTA1MDU2MCwic3ViIjoiZGV2ZWxvcGVyLzAzMmYzNDljLTgxYjQtZmExYy0xMGE4LTQwZmZkNDIzZDFiMCIsInNjb3BlcyI6WyJjbGFzaCJdLCJsaW1pdHMiOlt7InRpZXIiOiJkZXZlbG9wZXIvc2lsdmVyIiwidHlwZSI6InRocm90dGxpbmcifSx7ImNpZHJzIjpbIjY2LjE2OS4xNzYuMzAiLCIxNjguNjEuNDAuMTk1IiwiMTYyLjcxLjI0MS4xOCJdLCJ0eXBlIjoiY2xpZW50In1dfQ.TA-LBoLSpnBA9-Kxtv3Akq3lgn1M_JIfACv5Nx2Kg1qgIyJCnEU45QPrpmGaAPu3P8dVC-csaxsxrxGRp87s-g";
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
