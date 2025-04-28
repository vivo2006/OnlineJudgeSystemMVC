using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DataLayer;
using ServiceLayer.EntityServices;
using BusinessLayer.Entities;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class GroupCompetitionsController : Controller
    {
        private readonly GroupCompetitionsManager _manager;
        private readonly CompetitionsManager _competitionsManager;
        private readonly YearCompetitionsManager _yearCompetitionsManager;

        public GroupCompetitionsController(GroupCompetitionsManager manager, CompetitionsManager competitionsManager, YearCompetitionsManager yearCompetitionsManager)
        {
            _manager = manager;
            _competitionsManager = competitionsManager;
            _yearCompetitionsManager = yearCompetitionsManager;
        }

        // GET: GroupCompetitions/Index/?competitionId=5&yearId=5
        public async Task<IActionResult> Index(int competitionId, int yearId)
        {
            var yearCompetition = await _yearCompetitionsManager.ReadAsync(yearId, true);
            var competition = await _competitionsManager.ReadAsync(competitionId);

            ViewBag.Competition = competition.Name;
            ViewBag.Year = yearCompetition.Year;
            ViewBag.CompetitionNotation = competition.Notation;

            return View((List<GroupCompetition>)yearCompetition.GroupCompetitions);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var groupCompetition = await _manager.ReadAsync(id.Value);
            if (groupCompetition == null) return NotFound();

            return View(groupCompetition);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewData["CompetitionId"] = new SelectList(await _competitionsManager.ReadAllAsync(), "Id", "Name");

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Group,CompetitionId")] GroupCompetition groupCompetition)
        {
            if (!ModelState.IsValid) return View(groupCompetition);

            await _manager.CreateAsync(groupCompetition);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();

            var groupCompetition = await _manager.ReadAsync(id.Value);
            if (groupCompetition == null) return NotFound();

            return View(groupCompetition);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Group,CompetitionId")] GroupCompetition groupCompetition)
        {
            if (id != groupCompetition.Id) return BadRequest();

            if (!ModelState.IsValid) return View(groupCompetition);

            try
            {
                await _manager.UpdateAsync(groupCompetition);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await GroupCompetitionExists(groupCompetition.Id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var groupCompetition = await _manager.ReadAsync(id.Value);
            if (groupCompetition == null) return NotFound();

            return View(groupCompetition);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var groupCompetition = await _manager.ReadAsync(id);
            if (groupCompetition != null)
            {
                await _manager.DeleteAsync(id);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> GroupCompetitionExists(int id)
        {
            var groupCompetition = await _manager.ReadAsync(id);
            return groupCompetition != null;
        }
    }
}