using Newtonsoft.Json;
using OnlineJudgeWpfApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace OnlineJudgeWpfApp.Operations
{
    class TaskOperations : ApiOperations
    {
        // Get all tasks (tagId == 0) or belonging to tag with some ID
        public List<Task> GetTasks(int tagId = 0)
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
                List<Task> tasks = JsonConvert.DeserializeObject<List<Task>>(response);
                return tasks;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public List<Task> GetSolvedTasksByUser(int userId)
        {
            string endpoint = string.Format("{0}/task/solved/user/{1}", baseUrl, userId);

            WebClient wc = new WebClient();
            wc.Headers["Content-Type"] = "application/json";

            try
            {
                string response = wc.DownloadString(endpoint);
                List<Task> tasks = JsonConvert.DeserializeObject<List<Task>>(response);
                return tasks;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Task GetTaskDetails(int id)
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
                Task task = JsonConvert.DeserializeObject<Task>(response);
                
                // Convert to UTF8
                task.Description = Encoding.UTF8.GetString(Encoding.Default.GetBytes(task.Description));

                return task;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Task PostTask(Task task)
        {
            if (Globals.LoggedInUser == null)
            {
                return null;
            }

            // Convert to UTF8
            task.Description = Encoding.UTF8.GetString(Encoding.Default.GetBytes(task.Description));

            string endpoint = string.Format("{0}/task/", baseUrl);
            string method = "POST";
            string json = JsonConvert.SerializeObject(task);

            WebClient wc = new WebClient();
            wc.Headers["Content-Type"] = "application/json";
            wc.Headers["Authorization"] = string.Format("Bearer {0}", Globals.LoggedInUser.Token);

            try
            {
                string response = wc.UploadString(endpoint, method, json);
                return JsonConvert.DeserializeObject<Task>(response);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool PutTask(Task task, int id)
        {
            if (Globals.LoggedInUser == null)
            {
                return false;
            }

            // Convert to UTF8
            task.Description = Encoding.UTF8.GetString(Encoding.Default.GetBytes(task.Description));

            string endpoint = string.Format("{0}/task/{1}", baseUrl, id);
            string method = "PUT";
            string json = JsonConvert.SerializeObject(task);

            WebClient wc = new WebClient();
            wc.Headers["Content-Type"] = "application/json";
            wc.Headers["Authorization"] = string.Format("Bearer {0}", Globals.LoggedInUser.Token);

            try
            {
                string response = wc.UploadString(endpoint, method, json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
