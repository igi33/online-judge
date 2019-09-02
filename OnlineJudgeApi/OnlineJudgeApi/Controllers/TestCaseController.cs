using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeApi.Dtos;
using OnlineJudgeApi.Entities;
using OnlineJudgeApi.Helpers;

namespace OnlineJudgeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestCaseController : ControllerBase
    {
        private readonly DataContext _context;
        private IMapper mapper;

        public TestCaseController(DataContext context, IMapper mapper)
        {
            _context = context;
            this.mapper = mapper;
        }

        // GET: api/TestCase/task/5
        [HttpGet("task/{taskId}")]
        public async Task<IActionResult> GetTestCases(int taskId)
        {
            var testCases = await _context.TestCases.Where(tc => tc.TaskId == taskId).ToListAsync();
            var testCaseDtos = mapper.Map<IList<TestCaseDto>>(testCases);
            return Ok(testCaseDtos);
        }

        // PUT: api/TestCase/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTestCase(int id, TestCase testCase)
        {
            if (id != testCase.Id)
            {
                return BadRequest();
            }

            _context.Entry(testCase).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TestCaseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TestCase
        [HttpPost]
        public async Task<ActionResult<TestCase>> PostTestCase(TestCase testCase)
        {
            _context.TestCases.Add(testCase);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTestCase", new { id = testCase.Id }, testCase);
        }

        // DELETE: api/TestCase/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<TestCase>> DeleteTestCase(int id)
        {
            var testCase = await _context.TestCases.FindAsync(id);
            if (testCase == null)
            {
                return NotFound();
            }

            _context.TestCases.Remove(testCase);
            await _context.SaveChangesAsync();

            return testCase;
        }

        private bool TestCaseExists(int id)
        {
            return _context.TestCases.Any(e => e.Id == id);
        }
    }
}
