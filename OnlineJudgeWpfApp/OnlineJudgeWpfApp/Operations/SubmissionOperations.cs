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
    class SubmissionOperations : ApiOperations
    {
        private readonly string url;

        public SubmissionOperations()
        {
            url = baseUrl + "/submission";
        }

        public List<Submission> GetSubmissions(int taskId = 0, int userId = 0, int limit = 0, int offset = 0)
        {
            string endpoint = string.Format("{0}{1}", url, MakeQueryString(new Dictionary<string, object> {
                ["taskId"] = taskId,
                ["userId"] = userId,
                ["limit"] = limit,
                ["offset"] = offset,
            }));

            WebClient wc = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            wc.Headers["Content-Type"] = "application/json";

            if (Globals.LoggedInUser != null)
            {
                wc.Headers["Authorization"] = string.Format("Bearer {0}", Globals.LoggedInUser.Token);
            }

            try
            {
                string response = wc.DownloadString(endpoint);
                List<Submission> submissions = JsonConvert.DeserializeObject<List<Submission>>(response);
                return submissions;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public List<Submission> GetFastestSubmissionsOfTask(int taskId, int limit = 10)
        {
            string endpoint = string.Format("{0}/task/{1}/best{2}", url, taskId, MakeQueryString(new Dictionary<string, object>
            {
                ["limit"] = limit,
            }));

            WebClient wc = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            wc.Headers["Content-Type"] = "application/json";

            try
            {
                string response = wc.DownloadString(endpoint);
                List<Submission> submissions = JsonConvert.DeserializeObject<List<Submission>>(response);
                return submissions;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Submission PostSubmission(string sourceCode, int langId, int taskId)
        {
            if (Globals.LoggedInUser == null)
            {
                return null;
            }

            string endpoint = string.Format("{0}/task/{1}", url, taskId);
            string method = "POST";
            string json = JsonConvert.SerializeObject(new
            {
                sourcecode = sourceCode,
                langid = langId,
            });

            WebClient wc = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            wc.Headers["Content-Type"] = "application/json";
            wc.Headers["Authorization"] = string.Format("Bearer {0}", Globals.LoggedInUser.Token);

            try
            {
                string response = wc.UploadString(endpoint, method, json);
                Submission submission = JsonConvert.DeserializeObject<Submission>(response);
                return submission;
            }
            catch (Exception e)
            {
                return new Submission { Message = e.Message, Id = -1 };
            }
        }
    }
}
