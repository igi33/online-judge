using OnlineJudgeApi.Entities;
using OnlineJudgeApi.Dtos;
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

        // Compile a source code file into a binary file
        public static CompilationOutputDto Compile(ComputerLanguage lang, string sourceFileName, string binaryFileName)
        {
            string rootDir = GetExecutionRootDir();
            string sourceFilePath = $"{rootDir}{sourceFileName}";
            string binaryFilePath = $"{rootDir}{binaryFileName}";
            string compileCommand = $"{lang.CompilerFileName} {string.Format(lang.CompileCmd, sourceFilePath, binaryFilePath)}";

            BashExecutor executor = new BashExecutor(compileCommand);
            executor.Execute();

            return new CompilationOutputDto
            {
                ExitCode = (int)executor.ExitCode,
                Error = !string.IsNullOrEmpty(executor.Error) ? executor.Error.Replace(sourceFilePath, "") : "",
            };
        }

        // Run program, grade a single test case and return the results
        public static GradeDto Grade(ComputerLanguage lang, string fileName, string input, string expectedOutput, int timeLimitMs = 0, int memoryLimitB = 0)
        {
            string rootDir = GetExecutionRootDir();
            string timeOutputFilePath = $"{rootDir}time{fileName}.txt";

            // create cgroup
            $"sudo cgcreate -g memory,pids:{fileName}".Bash();

            // increase the initial memory limit for the cgroup a bit due to overhead and also set a minimum
            int pMemLimitB = Math.Max(memoryLimitB + 750000, 1150000);

            // set memory and max process limit
            $"sudo cgset -r memory.limit_in_bytes={pMemLimitB} -r memory.swappiness=0 -r pids.max=1 {fileName}".Bash();

            // timeout uses a longer time limit value because it measures real time and not cpu time.
            // we let the process run longer just in case, and after we inspect its cpu time from /usr/bin/time output.
            // timeout of zero means the associated timeout is disabled.
            float timeoutS = (timeLimitMs << 2) / 1000.0f;

            // prepare execution command string
            string escapedExecCmd = $"/usr/bin/time -p -o {timeOutputFilePath} sudo timeout --preserve-status {timeoutS} sudo cgexec -g memory,pids:{fileName} chroot --userspec=coderunner:no-network {rootDir} {string.Format(lang.ExecuteCmd, fileName)}".Replace("\"", "\\\"");

            // set initial return values
            GradeDto grade = new GradeDto
            {
                Status = "AC", // will stay accepted if test case passes
                ExecutionTime = 0,
                ExecutionMemory = 0,
                Output = "",
                Error = "",
            };

            using (Process q = new Process())
            {
                string output = "";
                string error = "";

                q.StartInfo.FileName = "/bin/bash";
                q.StartInfo.Arguments = $"-c \"{escapedExecCmd}\"";
                q.StartInfo.RedirectStandardInput = true;
                q.StartInfo.RedirectStandardOutput = true;
                q.StartInfo.RedirectStandardError = true;
                q.StartInfo.CreateNoWindow = true;
                q.StartInfo.UseShellExecute = false;
                q.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output += e.Data + "\n";
                    }
                });
                q.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error += e.Data + "\n";
                    }
                });

                q.Start();
                q.BeginOutputReadLine();
                q.BeginErrorReadLine();

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
                    // Unsuccessfully executed
                    grade.Error = error;

                    if (q.ExitCode == 137)
                    {
                        // cgroup sent SIGKILL
                        // the process exited with code 137
                        // meaning the memory limit was breached
                        grade.Status = "MLE";
                    }
                    else if (q.ExitCode == 143 || totalCpuTimeMs > timeLimitMs)
                    {
                        // timeout sent SIGTERM
                        // the process exited with code 137
                        // meaning the time limit was definitely breached
                        // OR
                        // actual CPU time breaches the limit
                        grade.Status = "TLE";
                        grade.ExecutionTime = timeLimitMs;
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
                    grade.Output = output;

                    // Check if submission output matches the expected output of test case
                    bool correctSoFar = true;

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
                        // Mismatch, set status as wrong answer
                        grade.Status = "WA";
                    }
                }
            }

            // get memory amount used
            string maxMemoryUsed = $"cgget -n -v -r memory.max_usage_in_bytes {fileName}".Bash().TrimEnd('\r', '\n');
            grade.ExecutionMemory = Int32.Parse(maxMemoryUsed);

            // delete cgroup
            $"sudo cgdelete -g memory,pids:{fileName}".Bash();
            
            // delete time output file
            System.IO.File.Delete(timeOutputFilePath);

            return grade;
        }
    }
}
