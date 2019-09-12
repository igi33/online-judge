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
    class TagOperations : ApiOperations
    {
        public List<Tag> GetTags()
        {
            string endpoint = string.Format("{0}/tag", baseUrl);

            WebClient wc = new WebClient();
            wc.Headers["Content-Type"] = "application/json";

            try
            {
                string response = wc.DownloadString(endpoint);
                List<Tag> tags = JsonConvert.DeserializeObject<List<Tag>>(response);
                return tags;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Tag GetTag(int tagId)
        {
            string endpoint = string.Format("{0}/tag/{1}", baseUrl, tagId);

            WebClient wc = new WebClient();
            wc.Headers["Content-Type"] = "application/json";

            try
            {
                string response = wc.DownloadString(endpoint);
                Tag tag = JsonConvert.DeserializeObject<Tag>(response);
                return tag;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
