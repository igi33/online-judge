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
    }
}
