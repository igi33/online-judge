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
    class TaskOperations : ApiOperations
    {
        // Get all tasks (tagId == 0) or belonging to tag with some ID
        public List<Models.Task> GetTasks(int tagId = 0)
        {
            string endpoint = tagId == 0 ? string.Format("{0}/task", baseUrl) : string.Format("{0}/task/tag/{1}", baseUrl, tagId);

            WebClient wc = new WebClient();
            wc.Headers["Content-Type"] = "application/json";

            if (Globals.LoggedInUser != null)
            {
                wc.Headers["Authorization"] = string.Format("Bearer {0}", Globals.LoggedInUser.Token);
            }

            try
            {
                string response = wc.DownloadString(endpoint);
                List<Models.Task> tasks = JsonConvert.DeserializeObject<List<Models.Task>>(response);
                return tasks;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Models.Task GetTaskDetails(int id)
        {
            string endpoint = string.Format("{0}/task/{1}", baseUrl, id);

            WebClient wc = new WebClient();
            wc.Headers["Content-Type"] = "application/json";

            if (Globals.LoggedInUser != null)
            {
                wc.Headers["Authorization"] = string.Format("Bearer {0}", Globals.LoggedInUser.Token);
            }

            try
            {
                string response = wc.DownloadString(endpoint);
                Models.Task task = JsonConvert.DeserializeObject<Models.Task>(response);
                return task;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
