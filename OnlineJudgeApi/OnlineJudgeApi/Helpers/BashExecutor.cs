using System;
using System.Diagnostics;

namespace OnlineJudgeApi.Helpers
{
    public class BashExecutor
    {
        protected string _command;
        public string Command
        {
            get { return _command; }
            set
            {
                _command = value;
                HasExecuted = false;
            }
        }
        protected int _waitForExitMs;
        public int WaitForExitMs
        {
            get { return _waitForExitMs; }
            set
            {
                _waitForExitMs = value;
                HasExecuted = false;
            }
        }
        public bool HasExecuted { get; private set; }
        public string Output { get; private set; }
        public string Error { get; private set; }
        public int? ExitCode { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime ExitTime { get; private set; }

        public BashExecutor(string cmd, int waitMs = 0)
        {
            this._command = cmd;
            this._waitForExitMs = waitMs;
            HasExecuted = false;
            Output = null;
            Error = null;
            ExitCode = null;
            StartTime = new DateTime();
            ExitTime = new DateTime();
        }

        public void Execute()
        {
            string escapedArgs = Command.Replace("\"", "\\\"");

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
                StartTime = DateTime.Now;
                process.Start();
                Output = process.StandardOutput.ReadToEnd();
                Error = process.StandardError.ReadToEnd();

                if (WaitForExitMs == 0)
                {
                    process.WaitForExit();
                }
                else
                {
                    if (!process.WaitForExit(WaitForExitMs))
                    {
                        process.Kill();
                    }
                }

                ExitCode = process.ExitCode;
                ExitTime = process.ExitTime;
            }

            HasExecuted = true;
        }
    }
}
