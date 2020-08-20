using System.Collections.Generic;
using System.Linq;

namespace OnlineJudgeWpfApp.Operations
{
    abstract class ApiOperations
    {
        /**
         * Base Url @string
         */
        protected readonly string baseUrl;

        public ApiOperations()
        {
            baseUrl = "http://localhost:4000/api";
        }

        public string MakeQueryString(IDictionary<string, object> parameters)
        {
            if (parameters.Count == 0)
            {
                return "";
            }

            return "?" + string.Join("&", parameters.Select(x => x.Key + "=" + x.Value.ToString()));
        }
    }
}
