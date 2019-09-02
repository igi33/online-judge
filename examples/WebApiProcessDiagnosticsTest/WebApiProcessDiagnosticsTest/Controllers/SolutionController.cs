using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace WebApiProcessDiagnosticsTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolutionController : ControllerBase
    {
        private const string CompilerName = "g++.exe";
        private const string CompilerPath = @"C:\mingw-w64\x86_64-8.1.0-posix-seh-rt_v6-rev0\mingw64\bin";
        private const string TaskName = "palin";
        private const int SolutionTime = 250; // ms
        private const int SolutionMemory = 64 * 1024; // B
        private const int NumberOfTestCases = 2;
        private static readonly string[] TaskInputs = { "5\nAb3bd\n", "10\nkatastrofa\n" };
        private static readonly string[] TaskOutputs = { "2", "5" };

        private void CompileAndRun(string solutionName)
        {
            Process p = new Process();
            p.StartInfo.EnvironmentVariables["CPATH"] = CompilerPath;
            p.StartInfo.FileName = CompilerName;
            p.StartInfo.Arguments = string.Format("-std=c++17 -O2 {0}.cpp -o {0}.exe", TaskName);
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.EnableRaisingEvents = true;
            p.StartInfo.RedirectStandardError = true;
            p.Exited += RunSolution;
            p.Start();
        }

        private void RunSolution(object sender, EventArgs e)
        {
            Process p = sender as Process;
            if (p.ExitCode != 0)
            {
                // compile error
                Debug.WriteLine("compile error: " + p.StandardError.ReadToEnd());
            }
            else
            {
                // compile success
                Debug.WriteLine("compile success: preparing to run " + NumberOfTestCases + " test cases");
                for (int i = 0; i < NumberOfTestCases; ++i)
                {
                    Process q = new Process();
                    q.StartInfo.FileName = string.Format("{0}.exe", TaskName);
                    q.StartInfo.RedirectStandardInput = true;
                    q.StartInfo.RedirectStandardOutput = true;
                    q.StartInfo.RedirectStandardError = true;
                    q.StartInfo.CreateNoWindow = false;
                    q.StartInfo.UseShellExecute = false;
                    q.Start();

                    StreamWriter inputWriter = q.StandardInput;
                    inputWriter.Write(TaskInputs[i]);
                    inputWriter.Close();

                    bool exited = q.WaitForExit(SolutionTime);
                    if (!exited)
                    {
                        // time limit exceeded for whatever reason
                        // rejected
                        q.Kill();
                        Debug.WriteLine("TLE with error: " + q.StandardError.ReadToEnd());
                    }
                    else
                    {
                        if (q.ExitCode != 0)
                        {
                            // runtime error
                            // rejected
                            Debug.WriteLine("run error:\nreturn code is " + q.ExitCode + " with error: " + q.StandardError.ReadToEnd());
                        }
                        else
                        {
                            // success
                            // accepted
                            string output = q.StandardOutput.ReadToEnd();
                            double executionTimeMs = q.ExitTime.Subtract(q.StartTime).TotalMilliseconds;
                            Debug.WriteLine("run success:\noutput: " + output + "correct output: " + TaskOutputs[i] + ", execution time (ms): " + executionTimeMs);
                        }
                    }
                    q.Close();
                }
            }
            p.Close();
        }

        // POST: api/Solution
        [HttpPost]
        public void Post([FromBody] string value)
        {
            CompileAndRun(value); // value from body unused
        }
    }
}
