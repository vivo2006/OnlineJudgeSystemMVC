using OnlineJudgeSystemMVC.Presentation;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataLayer;
using ServiceLayer.EntityServices;
using BusinessLayer.Entities;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class YearCompetitionsController : Controller
    {
        private readonly YearCompetitionsManager _yearManager;
        private readonly GroupCompetitionsManager _groupManager;
        private readonly CompetitionsManager _competitionManager;

        public YearCompetitionsController(YearCompetitionsManager yearManager, GroupCompetitionsManager groupManager, CompetitionsManager competitionManager)
        {
            _yearManager = yearManager;
            _groupManager = groupManager;
            _competitionManager = competitionManager;
        }

        public IActionResult Index() => Redirect("/Competitions/Index");

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int id)
        {
            var competition = await _competitionManager.ReadAsync(id);
            ViewBag.CompetitionId = id;
            ViewBag.Competition = EnglishContent.CompetitionName(competition);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Year,CompetitionId")] YearCompetition yearCompetition)
        {
            ViewBag.CompetitionId = yearCompetition.CompetitionId;
            ViewBag.Competition = EnglishContent.CompetitionName(await _competitionManager.ReadAsync(yearCompetition.CompetitionId));

            if ((await _yearManager.ReadAllAsync()).Any(c => c.Year == yearCompetition.Year))
                ModelState.AddModelError("", "The year must be unique within the competition.");

            if (!ModelState.IsValid)
                return View(yearCompetition);

            var competition = await _competitionManager.ReadAsync(yearCompetition.CompetitionId, true);
            await _yearManager.CreateAsync(yearCompetition);

            competition.YearCompetitions.Add(yearCompetition);
            await _competitionManager.UpdateAsync(competition);

            for (int i = 0; i < 5; i++)
            {
                var group = new GroupCompetition(((char)(i + 'A')).ToString(), yearCompetition.Id);
                await _groupManager.CreateAsync(group);
                yearCompetition.GroupCompetitions.Add(group);
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();

            var yearCompetition = await _yearManager.ReadAsync(id.Value, true);
            if (yearCompetition == null) return NotFound();

            ViewBag.CompetitionName = EnglishContent.CompetitionName(yearCompetition.Competition);
            return View(yearCompetition);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Year,CompetitionId")] YearCompetition yearCompetition)
        {
            if (id != yearCompetition.Id) return BadRequest();

            if (!ModelState.IsValid)
                return View(yearCompetition);

            try
            {
                await _yearManager.UpdateAsync(yearCompetition);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await YearCompetitionExists(yearCompetition.Id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var yearCompetition = await _yearManager.ReadAsync(id.Value);
            return yearCompetition == null ? NotFound() : View(yearCompetition);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();

            var yearCompetition = await _yearManager.ReadAsync(id.Value, true);
            return yearCompetition == null ? NotFound() : View(yearCompetition);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var yearCompetition = await _yearManager.ReadAsync(id, true);
            await _yearManager.DeleteAsync(id);

            var competition = yearCompetition.Competition;
            _yearManager.DeleteDirectory("Problems", competition.Notation, yearCompetition.Year.ToString(), prefix: Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            _yearManager.DeleteDirectory("TestCases", competition.Notation, yearCompetition.Year.ToString(), prefix: Directory.GetCurrentDirectory());

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> YearCompetitionExists(int id) => await _yearManager.ReadAsync(id) != null;
    }
}