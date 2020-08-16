using System.Diagnostics;

namespace OnlineJudgeApi.Helpers
{
    public static class ShellHelper
    {
        public static string Bash(this string cmd, int waitMs = 0)
        {
            string escapedArgs = cmd.Replace("\"", "\\\"");
            string output = "";
            string error = "";

            using (Process process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            })
            {
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();

                if (waitMs == 0)
                {
                    process.WaitForExit();
                }
                else
                {
                    process.WaitForExit(waitMs);
                }
            }

            return string.IsNullOrEmpty(error) ? output : error;
        }
    }
}
