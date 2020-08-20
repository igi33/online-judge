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
    class UserOperations : ApiOperations
    {
        private readonly string url;

        public UserOperations()
        {
            url = baseUrl + "/user";
        }

        /**
         * Authenticate user with Web Api Endpoint
         * @param string username
         * @param string password
         */
        public User AuthenticateUser(string username, string password)
        {
            string endpoint = string.Format("{0}/authenticate", url);
            string method = "POST";
            string json = JsonConvert.SerializeObject(new
            {
                username,
                password
            });

            WebClient wc = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            wc.Headers["Content-Type"] = "application/json";
            try
            {
                string response = wc.UploadString(endpoint, method, json);
                return JsonConvert.DeserializeObject<User>(response);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /**
         * Get User Details from Web Api
         * @param  User Model
         */
        public User GetUserDetails(int userId)
        {
            string endpoint = string.Format("{0}/{1}", url, userId);
            //string access_token = user.Token;

            WebClient wc = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            wc.Headers["Content-Type"] = "application/json";
            try
            {
                string response = wc.DownloadString(endpoint);
                User user = JsonConvert.DeserializeObject<User>(response);
                return user;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /**
         * Register User
         * @param  string username
         * @param  string password
         * @param  string email
         */
        public User RegisterUser(string username, string password, string email)
        {
            string endpoint = url;
            string method = "POST";
            string json = JsonConvert.SerializeObject(new
            {
                username,
                password,
                email
            });

            WebClient wc = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            wc.Headers["Content-Type"] = "application/json";
            try
            {
                string response = wc.UploadString(endpoint, method, json);
                return JsonConvert.DeserializeObject<User>(response);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
