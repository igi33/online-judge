using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeApi.Dtos;
using OnlineJudgeApi.Helpers;
using System.Diagnostics;

namespace OnlineJudgeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComputerLanguageController : ControllerBase
    {
        private readonly DataContext _context;
        private IMapper mapper;

        public ComputerLanguageController(DataContext context, IMapper mapper)
        {
            _context = context;
            this.mapper = mapper;
        }

        // GET: api/ComputerLanguage
        [HttpGet]
        public async Task<IActionResult> GetLangs()
        {
            var langs = await _context.ComputerLanguages.ToListAsync();
            var langDtos = mapper.Map<IList<ComputerLanguageDto>>(langs);
            return Ok(langDtos);
        }

        // FOR TESTING BASH
        // TODO: REMOVE LATER
        // POST: api/ComputerLanguage
        [HttpPost]
        public IActionResult PostLang()
        {
            string msg = "";

            /*
            // Compile, execute and grade submission
            using (Process p = new Process())
            {
                //p.StartInfo.FileName = "/bin/bash";
                //p.StartInfo.Arguments = "-c \"ls /home/igi33/\"";
                p.StartInfo.FileName = "ls";
                p.StartInfo.Arguments = "/home/igi33/";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();
                while (!p.StandardOutput.EndOfStream)
                {
                    string line = p.StandardOutput.ReadLine();
                    msg += "output: " + line + "\n";
                }
                while (!p.StandardError.EndOfStream)
                {
                    string line = p.StandardError.ReadLine();
                    msg += "error: " + line + "\n";
                }
            }
            */


            string id = "palin";

            "sudo cgcreate -g memory:test".Bash();
            "sudo cgset -r memory.limit_in_bytes=67108864 -r memory.swappiness=0 test".Bash();

            string command = string.Format("sudo cgexec -g memory:test chroot ~/executionroot/ ./{0}", id);
            BashExecutor executor = new BashExecutor(command, 250);
            executor.Execute();

            msg = string.IsNullOrEmpty(executor.Error) ? executor.Output : executor.Error;

            int elapsed = (int)executor.ExitTime.Subtract(executor.StartTime).TotalMilliseconds;

            string memoryUsed = "cgget -n -v -r memory.max_usage_in_bytes test".Bash();
            "sudo cgdelete -g memory:test".Bash();
            
            return Ok(new { ExitCode = executor.ExitCode, Msg = msg, MemoryUsed = memoryUsed, MsElapsed = elapsed });
            

            /*
            const string rootDir = @"/home/igi33/executionroot/";
            string submissionId = 25.ToString();
            string sourceFileName = string.Format("{0}.{1}", submissionId, "cpp");

            string binaryFilePath = string.Format("{0}{1}", rootDir, submissionId);
            string sourceFilePath = string.Format("{0}{1}", rootDir, sourceFileName);

            // Create file from source code inside rootDir
            System.IO.File.WriteAllText(sourceFilePath, "this is source code!");
            
            return Ok(new { Msg = msg, MemoryUsed = 1 });
            */
        }
    }
}