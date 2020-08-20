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
        private readonly string url;

        public TaskOperations()
        {
            url = baseUrl + "/task";
        }

        // Get tasks possibly paged and filtered by tag id
        public List<Task> GetTasks(int tagId = 0, int limit = 0, int offset = 0)
        {
            string endpoint = string.Format("{0}{1}", url, MakeQueryString(new Dictionary<string, object>
            {
                ["tagId"] = tagId,
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
                List<Task> tasks = JsonConvert.DeserializeObject<List<Task>>(response);
                return tasks;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public List<Task> GetSolvedTasksByUser(int userId, int limit = 0, int offset = 0)
        {
            string endpoint = string.Format("{0}/solvedby/{1}{2}", url, userId, MakeQueryString(new Dictionary<string, object>
            {
                ["limit"] = limit,
                ["offset"] = offset
            }));

            WebClient wc = new WebClient
            {
                Encoding = Encoding.UTF8
            };
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
            string endpoint = string.Format("{0}/{1}", url, id);

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
                Task task = JsonConvert.DeserializeObject<Task>(response);
                
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

            string endpoint = url;
            string method = "POST";
            string json = JsonConvert.SerializeObject(task);

            WebClient wc = new WebClient
            {
                Encoding = Encoding.UTF8
            };
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

            string endpoint = string.Format("{0}/{1}", url, id);
            string method = "PUT";
            string json = JsonConvert.SerializeObject(task);

            WebClient wc = new WebClient
            {
                Encoding = Encoding.UTF8
            };
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
