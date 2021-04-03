using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineJudgeApi.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeApi.Entities;

namespace OnlineJudgeApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class TestCaseController : Controller
    {
        private readonly DataContext _context;
        private readonly IMapper mapper;

        public TestCaseController(DataContext context, IMapper mapper)
        {
            _context = context;
            this.mapper = mapper;
        }

        // Return test case INPUT by id as a text file
        // GET: api/TestCase/5/input
        [HttpGet("{id}/input")]
        public async Task<IActionResult> GetTestCaseInput(int id)
        {
            if (!await _context.TestCases.AnyAsync(tc => tc.Id == id))
            {
                return NotFound();
            }

            int tcUserId = await _context
                .TestCases
                .Where(tc => tc.Id == id)
                .Select(tc => tc.Task.User.Id)
                .SingleOrDefaultAsync();

            int currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);

            if (currentUserId != tcUserId)
            {
                return Unauthorized();
            }

            string input = await _context
                .TestCases
                .Where(tc => tc.Id == id)
                .Select(tc => tc.Input)
                .SingleOrDefaultAsync();

            return File(Encoding.UTF8.GetBytes(input), "text/plain");
        }

        // Return test case OUTPUT by id as a text file
        // GET: api/TestCase/5/output
        [HttpGet("{id}/output")]
        public async Task<IActionResult> GetTestCaseOutput(int id)
        {
            if (!await _context.TestCases.AnyAsync(tc => tc.Id == id))
            {
                return NotFound();
            }

            int tcUserId = await _context
                .TestCases
                .Where(tc => tc.Id == id)
                .Select(tc => tc.Task.User.Id)
                .SingleOrDefaultAsync();

            int currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);
            if (currentUserId != tcUserId)
            {
                return Unauthorized();
            }

            string output = await _context
                .TestCases
                .Where(tc => tc.Id == id)
                .Select(tc => tc.Output)
                .SingleOrDefaultAsync();

            return File(Encoding.UTF8.GetBytes(output), "text/plain");
        }

        // DELETE: api/TestCase/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTestCase(int id)
        {
            if (!await _context.TestCases.AnyAsync(tc => tc.Id == id))
            {
                return NotFound();
            }

            int tcUserId = await _context
                .TestCases
                .Where(tc => tc.Id == id)
                .Select(tc => tc.Task.User.Id)
                .SingleOrDefaultAsync();

            int currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);

            if (currentUserId != tcUserId)
            {
                return Unauthorized();
            }

            TestCase tc = new TestCase() { Id = id };
            _context.TestCases.Attach(tc);
            _context.TestCases.Remove(tc);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
