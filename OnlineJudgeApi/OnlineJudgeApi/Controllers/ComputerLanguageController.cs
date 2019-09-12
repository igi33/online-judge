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
    }
}