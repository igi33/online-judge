using Newtonsoft.Json;
using OnlineJudgeWpfApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OnlineJudgeWpfApp.Operations
{
    class ComputerLanguageOperations : ApiOperations
    {
        private readonly string url;

        public ComputerLanguageOperations()
        {
            url = baseUrl + "/computerlanguage";
        }

        public List<ComputerLanguage> GetLangs()
        {
            string endpoint = url;

            WebClient wc = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            wc.Headers["Content-Type"] = "application/json";

            try
            {
                string response = wc.DownloadString(endpoint);
                List<ComputerLanguage> langs = JsonConvert.DeserializeObject<List<ComputerLanguage>>(response);
                return langs;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
