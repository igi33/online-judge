using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeApi.Dtos;
using OnlineJudgeApi.Helpers;

namespace OnlineJudgeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComputerLanguageController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IMapper mapper;

        public ComputerLanguageController(DataContext context, IMapper mapper)
        {
            _context = context;
            this.mapper = mapper;
        }

        // GET: api/ComputerLanguage
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComputerLanguageDto>>> GetLangs()
        {
            var langs = await _context.ComputerLanguages.ToListAsync();
            var langDtos = mapper.Map<IList<ComputerLanguageDto>>(langs);
            return Ok(langDtos);
        }
    }
}