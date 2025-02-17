using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BusinessLayer;
using DataLayer;
using ServiceLayer;
using Microsoft.AspNetCore.Authorization;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class GroupCompetitionsController : Controller
    {
        private readonly GroupCompetitionsManager manager;
        private readonly CompetitionsManager competitionsManager;
        private readonly YearCompetitionsManager yearCompetitionsManager;

        public GroupCompetitionsController(GroupCompetitionsManager manager, CompetitionsManager competitionsManager, YearCompetitionsManager yearCompetitionsManager)
        {
            this.manager = manager;
            this.competitionsManager = competitionsManager;
            this.yearCompetitionsManager = yearCompetitionsManager;
        }

        // GET: GroupCompetitions/Index/?competitionId=5&yearId=5
        // GET: GroupCompetitions from YearCompetition
        public async Task<IActionResult> Index(int competitionId, int yearId)
        {
            YearCompetition yearCompetition = await yearCompetitionsManager.ReadAsync(yearId, true);
            List<GroupCompetition> groupCompetitions = (List<GroupCompetition>)yearCompetition.GroupCompetitions;
            Competition competition = await competitionsManager.ReadAsync(competitionId);
            ViewBag.Competition = competition.Name;
            ViewBag.Year = yearCompetition.Year;
            ViewBag.CompetitionNotation = competition.Notation;
            return View(groupCompetitions);
        }

        // GET: GroupCompetitions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var groupCompetition = await manager.ReadAsync((int)id);
            if (groupCompetition == null)
            {
                return NotFound();
            }

            return View(groupCompetition);
        }
        [Authorize(Roles = "Admin")]

        // GET: GroupCompetitions/Create
        public async Task<IActionResult> Create()
        { 
            ViewData["CompetitionId"] = new SelectList(await competitionsManager.ReadAllAsync(), "Id", "Name");
            return View();
        }

        // POST: GroupCompetitions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Group,CompetitionId")] GroupCompetition groupCompetition)
        {
            if (ModelState.IsValid)
            {
                await manager.CreateAsync(groupCompetition);
                return RedirectToAction(nameof(Index));
            }
            //ViewData["CompetitionId"] = new SelectList(await competitionsManager.ReadAllAsync(), "Id", "Name", groupCompetition.CompetitionId);
            return View(groupCompetition);
        }
        [Authorize(Roles = "Admin")]
        // GET: GroupCompetitions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var groupCompetition = await manager.ReadAsync((int)id);
            if (groupCompetition == null)
            {
                return NotFound();
            }
            //ViewData["CompetitionId"] = new SelectList(await competitionsManager.ReadAllAsync(), "Id", "Name", groupCompetition.CompetitionId);
            return View(groupCompetition);
        }
        [Authorize(Roles = "Admin")]
        // POST: GroupCompetitions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Group,CompetitionId")] GroupCompetition groupCompetition)
        {
            if (id != groupCompetition.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await manager.UpdateAsync(groupCompetition);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupCompetitionExists(groupCompetition.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(groupCompetition);
        }
        [Authorize(Roles = "Admin")]
        // GET: GroupCompetitions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var groupCompetition = await manager.ReadAsync((int)id);
            if (groupCompetition == null)
            {
                return NotFound();
            }

            return View(groupCompetition);
        }

        // POST: GroupCompetitions/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var groupCompetition = await manager.ReadAsync((int)id);
            if (groupCompetition != null)
            {
                await manager.DeleteAsync(id);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool GroupCompetitionExists(int id)
        {
            return manager.ReadAsync(id) != null;
        }
    }
}
