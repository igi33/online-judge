﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using OnlineJudgeApi.Dtos;
using OnlineJudgeApi.Entities;
using OnlineJudgeApi.Helpers;

namespace OnlineJudgeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GradeController : ControllerBase
    {
        private readonly DataContext _context;

        public GradeController(DataContext context)
        {
            _context = context;
        }

        // POST: api/Grade
        [HttpPost]
        public async Task<ActionResult<GradeDto>> Grade([FromBody] GradeSubmissionDto gradeSubmissionDto)
        {
            // Get language info
            ComputerLanguage lang = await _context.ComputerLanguages.FindAsync(gradeSubmissionDto.LangId);
            if (lang == null)
            {
                return BadRequest(new { Message = "The supplied computer language id doesn't exist!" });
            }

            string executionRootDir = Grader.GetExecutionRootDir();
            string randomId = RandomString(32);

            string sourceFileName = $"{randomId}.{lang.Extension}";
            string binaryFileName = randomId;

            string sourceFilePath = $"{executionRootDir}{sourceFileName}";
            string binaryFilePath = $"{executionRootDir}{binaryFileName}";

            // Create file from source code inside rootDir
            System.IO.File.WriteAllText(sourceFilePath, gradeSubmissionDto.SourceCode);

            GradeDto result = new GradeDto(); // this will be returned by the method

            bool readyToRun = true;

            // Check if compiled language
            if (!string.IsNullOrEmpty(lang.CompileCmd))
            {
                // Compile submission
                CompilationOutputDto co = Grader.Compile(lang, sourceFileName, binaryFileName);

                if (co.ExitCode != 0)
                {
                    // Compile error
                    result.Status = "CE"; // Mark status as Compile Error
                    result.Error = co.Error; // Set message as compile error message
                    readyToRun = false;
                }
            }
            
            if (readyToRun)
            {
                // Compiled successfully or interpreted language, so we're ready to run the solution

                string fileName = string.IsNullOrEmpty(lang.CompileCmd) ? sourceFileName : binaryFileName;

                // Grade solution
                result = Grader.Grade(lang, fileName, gradeSubmissionDto.Input, gradeSubmissionDto.ExpectedOutput, gradeSubmissionDto.TimeLimit, gradeSubmissionDto.MemoryLimit);

                // Delete binary file
                System.IO.File.Delete(binaryFilePath);
            }

            // Delete source file
            System.IO.File.Delete(sourceFilePath);

            return Ok(result);
        }

        private static readonly Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
