using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessLayer.Entities;
using DataLayer;
using ServiceLayer.EntityServices;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class CompetitionsController : Controller
    {
        private readonly CompetitionsManager _manager;
        private readonly YearCompetitionsManager _yearCompetitionsManager;
        private readonly GroupCompetitionsManager _groupCompetitionsManager;

        public CompetitionsController(CompetitionsManager manager, YearCompetitionsManager yearCompetitionsManager, GroupCompetitionsManager groupCompetitionsManager)
        {
            _manager = manager;
            _yearCompetitionsManager = yearCompetitionsManager;
            _groupCompetitionsManager = groupCompetitionsManager;
        }

        // GET: Competitions
        public async Task<IActionResult> Index()
            => View(await _manager.ReadAllAsync());

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdministratorIndex()
            => View(await _manager.ReadAllAsync());

        [Route("api/competitions/{id}/years")]
        [HttpGet]
        public async Task<IActionResult> GetYears(int id)
        {
            var competition = await _manager.ReadAsync(id, true);
            if (competition == null) return NotFound();

            var years = competition.YearCompetitions.Select(x => Tuple.Create(x.Id, x.Year)).ToList();

            return Ok(years);
        }

        public async Task<IActionResult> IndexYears(int id)
        {
            var competition = await _manager.ReadAsync(id, true);
            return competition == null ? NotFound() : View(competition.YearCompetitions);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var competition = await _manager.ReadAsync(id.Value);
            return competition == null ? NotFound() : View(competition);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
            => View();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,FirstYear,LastYear,Notation")] Competition competition)
        {
            if ((await _manager.ReadAllAsync()).Any(c => c.Notation == competition.Notation))
                ModelState.AddModelError("", "The identifier must be unique.");

            if (competition.FirstYear > competition.LastYear)
                ModelState.AddModelError("", "The first year must not be later than the last year.");

            if (!ModelState.IsValid)
                return View(competition);

            await _manager.CreateAsync(competition);

            var yearCompetitions = new List<YearCompetition>();
            for (int year = competition.FirstYear; year <= competition.LastYear; year++)
            {
                var yearCompetition = new YearCompetition
                {
                    Year = year,
                    CompetitionId = competition.Id
                };
                await _yearCompetitionsManager.CreateAsync(yearCompetition);

                
                for(int i = 0; i < 5; i ++)
                {
                    yearCompetition.GroupCompetitions.Add(new GroupCompetition(((char)('A' + i)).ToString(), yearCompetition.Id));
                }
                await _groupCompetitionsManager.CreateAsync(yearCompetition.GroupCompetitions.First());

                await _yearCompetitionsManager.UpdateAsync(yearCompetition);
                yearCompetitions.Add(yearCompetition);
            }

            competition.YearCompetitions = yearCompetitions;
            await _manager.UpdateAsync(competition, true);

            return RedirectToAction(nameof(AdministratorIndex));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var competition = await _manager.ReadAsync(id.Value);
            return competition == null ? NotFound() : View(competition);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstYear,LastYear,Name,Notation")] Competition competition)
        {
            if (id != competition.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(competition);

            try
            {
                await _manager.UpdateAsync(competition);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CompetitionExists(competition.Id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(AdministratorIndex));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var competition = await _manager.ReadAsync(id.Value);
            return competition == null ? NotFound() : View(competition);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var competition = await _manager.ReadAsync(id);
            if (competition != null)
            {
                await _manager.DeleteAsync(id);
                _manager.DeleteDirectory("Problems", competition.Notation, prefix: Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
                _manager.DeleteDirectory("TestCases", competition.Notation, prefix: Directory.GetCurrentDirectory());
            }

            return RedirectToAction(nameof(AdministratorIndex));
        }

        private async Task<bool> CompetitionExists(int id)
            => await _manager.ReadAsync(id) != null;
    }
}