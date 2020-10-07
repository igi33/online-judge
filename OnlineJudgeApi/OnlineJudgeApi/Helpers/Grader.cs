using OnlineJudgeApi.Entities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace OnlineJudgeApi.Helpers
{
    public static class Grader
    {
        // Get linux root dir from where solutions are compiled and executed
        public static string GetExecutionRootDir()
        {
            string cmdUsername = "whoami".Bash().Trim();
            return $"/home/{cmdUsername}/executionroot/";
        }

        // Compile an already existing source code file into a binary file with the name supplied
        public static CompilationOutput Compile(ComputerLanguage lang, string binaryFileName)
        {
            string rootDir = GetExecutionRootDir();
            string binaryFilePath = $"{rootDir}{binaryFileName}";

            BashExecutor executor = new BashExecutor($"{lang.CompilerFileName} {string.Format(lang.CompileCmd, binaryFilePath)}");
            executor.Execute();

            return new CompilationOutput
            {
                ExitCode = (int)executor.ExitCode,
                Message = executor.Error ?? "",
            };
        }

        // Grade a single test case with an already compiled source code file (binaryFileName) and return the results
        public static Grade Grade(string binaryFileName, string input, string expectedOutput, int timeLimit = 0, int memoryLimit = 0)
        {
            string rootDir = GetExecutionRootDir();
            string timeOutputFilePath = $"{rootDir}time{binaryFileName}.txt";

            // create cgroup
            $"sudo cgcreate -g memory:{binaryFileName}".Bash();

            // increase the initial memory limit for the cgroup a bit due to overhead and also set a minimum
            int pMemLimitB = Math.Max(memoryLimit + 750000, 1150000);

            // set memory limit a bit higher than task parameter
            $"sudo cgset -r memory.limit_in_bytes={pMemLimitB} -r memory.swappiness=0 {binaryFileName}".Bash();

            // timeout uses a longer time limit value because it measures real time and not cpu time.
            // we let the process run longer just in case, and after we inspect its cpu time from /usr/bin/time output
            float timeoutS = (timeLimit << 2) / 1000.0f;

            // prepare execution command string
            string escapedExecCmd = $"/usr/bin/time -p -o {timeOutputFilePath} sudo timeout --preserve-status {timeoutS} sudo cgexec -g memory:{binaryFileName} chroot {rootDir} ./{binaryFileName}".Replace("\"", "\\\"");

            // set initial return values
            Grade grade = new Grade
            {
                Status = "AC", // will stay accepted if test case passes
                ExecutionTime = 0,
                ExecutionMemory = 0,
            };

            using (Process q = new Process())
            {
                string output = "";

                q.StartInfo.FileName = "/bin/bash";
                q.StartInfo.Arguments = $"-c \"{escapedExecCmd}\"";
                q.StartInfo.RedirectStandardInput = true;
                q.StartInfo.RedirectStandardOutput = true;
                q.StartInfo.CreateNoWindow = false;
                q.StartInfo.UseShellExecute = false;
                q.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output += e.Data + "\n";
                    }
                });

                q.Start();
                q.BeginOutputReadLine();

                StreamWriter inputWriter = q.StandardInput;
                inputWriter.Write(input);
                inputWriter.Close();

                q.WaitForExit();

                // fetch and check if execution CPU time actually meets the limit
                double userCpuTimeS = double.Parse($"grep -oP '(?<=user ).*' {timeOutputFilePath}".Bash());
                double sysCpuTimeS = double.Parse($"grep -oP '(?<=sys ).*' {timeOutputFilePath}".Bash());
                int totalCpuTimeMs = (int)((userCpuTimeS + sysCpuTimeS) * 1000 + 0.5); // round
                grade.ExecutionTime = totalCpuTimeMs;

                if (q.ExitCode != 0)
                {
                    if (q.ExitCode == 137)
                    {
                        // cgroup sent SIGKILL
                        // the process exited with code 137
                        // meaning the memory limit was breached
                        grade.Status = "MLE";
                    }
                    else if (q.ExitCode == 143 || totalCpuTimeMs > timeLimit)
                    {
                        // timeout sent SIGTERM
                        // the process exited with code 137
                        // meaning the time limit was definitely breached
                        // OR
                        // actual CPU time breaches the limit
                        grade.Status = "TLE";
                        grade.ExecutionTime = timeLimit;
                    }
                    else
                    {
                        // Runtime error
                        // Rejected
                        grade.Status = "RTE";
                    }
                }
                else
                {
                    // Successfully executed

                    bool correctSoFar = true;

                    // Check if submission output matches the expected output of test case
                    string[] outputLines = output.Trim().Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    );
                    string[] tcOutputLines = expectedOutput.Trim().Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    );

                    if (outputLines.Length != tcOutputLines.Length)
                    {
                        correctSoFar = false;
                    }

                    int idx = 0;
                    while (correctSoFar && idx < outputLines.Length)
                    {
                        if (!outputLines.ElementAt(idx).Equals(tcOutputLines.ElementAt(idx)))
                        {
                            correctSoFar = false;
                        }
                        ++idx;
                    }

                    if (!correctSoFar)
                    {
                        // Mismatch, set status as rejected
                        grade.Status = "RJ";
                    }
                }
            }

            // get memory amount used
            string maxMemoryUsed = $"cgget -n -v -r memory.max_usage_in_bytes {binaryFileName}".Bash().TrimEnd('\r', '\n');
            grade.ExecutionMemory = Int32.Parse(maxMemoryUsed);

            // delete cgroup
            $"sudo cgdelete -g memory:{binaryFileName}".Bash();
            
            // delete time output file
            System.IO.File.Delete(timeOutputFilePath);

            return grade;
        }
    }
}
