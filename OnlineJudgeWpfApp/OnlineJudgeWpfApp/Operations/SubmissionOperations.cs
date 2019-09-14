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
    }
}
