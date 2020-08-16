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
        public List<Submission> GetSubmissions(int taskId = 0, int userId = 0)
        {
            string endpoint = string.Format("{0}/submission/task/{1}/user/{2}", baseUrl, taskId, userId);

            WebClient wc = new WebClient();
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

        public List<Submission> GetFastestSubmissionsOfTask(int taskId)
        {
            string endpoint = string.Format("{0}/submission/task/{1}/best", baseUrl, taskId);

            WebClient wc = new WebClient();
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

            string endpoint = string.Format("{0}/submission/task/{1}", baseUrl, taskId);
            string method = "POST";
            string json = JsonConvert.SerializeObject(new
            {
                sourcecode = sourceCode,
                langid = langId,
            });

            WebClient wc = new WebClient();
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
