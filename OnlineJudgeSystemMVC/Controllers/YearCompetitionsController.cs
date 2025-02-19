using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DataLayer;
using ServiceLayer.EntityServices;
using BusinessLayer.Entities;
using Microsoft.AspNetCore.Authorization;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class YearCompetitionsController : Controller
    {
        private readonly YearCompetitionsManager manager;
        private readonly GroupCompetitionsManager groupCompetitionsManager;
        private readonly CompetitionsManager competitionsManager;

        public YearCompetitionsController(YearCompetitionsManager manager, GroupCompetitionsManager groupCompetitionsManager, CompetitionsManager competitionsManager)
        {
            this.manager = manager;
            this.groupCompetitionsManager = groupCompetitionsManager;
            this.competitionsManager = competitionsManager;
        }

        // GET: YearCompetitions
        public  IActionResult Index()
        {
            return Redirect("/Competitions/Index");
        }
        [Authorize(Roles = "Admin")]
        // GET: YearCompetitions/Create
        public async Task<IActionResult> Create(int id)
        {
            ViewBag.CompetitionId = id;
            ViewBag.Competition = (await competitionsManager.ReadAsync(id)).Name;
            return View();
        }

        // POST: YearCompetitions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Year,CompetitionId")] YearCompetition yearCompetition)
        {
            ViewBag.CompetitionId = yearCompetition.CompetitionId;
            ViewBag.Competition = (await competitionsManager.ReadAsync(yearCompetition.CompetitionId)).Name;
            if ((await manager.ReadAllAsync()).Any(c => c.Year == yearCompetition.Year)) ModelState.AddModelError("", "Годината трябва да е уникална.");
            if (ModelState.IsValid)
            {                
                Competition competition = await competitionsManager.ReadAsync(yearCompetition.CompetitionId, true);
                await manager.CreateAsync(yearCompetition);
                competition.YearCompetitions.Add(yearCompetition);
                await competitionsManager.UpdateAsync(competition);
                for (int i = 0; i < 5; i++)
                {
                    var groupCompetition = new GroupCompetition(((char)(i + 'A')).ToString(), yearCompetition.Id);
                    await groupCompetitionsManager.CreateAsync(groupCompetition);
                    yearCompetition.GroupCompetitions.Add(groupCompetition);
                }
                return RedirectToAction(nameof(Index));
            }
            return View(yearCompetition);
        }

        // GET: YearCompetitions/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var yearCompetition = await manager.ReadAsync((int)id, true);
            ViewBag.CompetitionName = yearCompetition.Competition.Name;
            if (yearCompetition == null)
            {
                return NotFound();
            }
            return View(yearCompetition);
        }

        // POST: YearCompetitions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Year,CompetitionId")] YearCompetition yearCompetition)
        {
            if (id != yearCompetition.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await manager.UpdateAsync(yearCompetition);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!YearCompetitionExists(yearCompetition.Id))
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
            return View(yearCompetition);
        }

        // GET: YearCompetitions/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var yearCompetition = await manager.ReadAsync((int)id);
            if (yearCompetition == null)
            {
                return NotFound();
            }

            return View(yearCompetition);
        }

        // GET: YearCompetitions/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var yearCompetition = await manager.ReadAsync((int)id, true);
            if (yearCompetition == null)
            {
                return NotFound();
            }

            return View(yearCompetition);
        }

        // POST: YearCompetitions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            YearCompetition yearCompetition = await manager.ReadAsync(id, true);            
            await manager.DeleteAsync(id);
            Competition competition = yearCompetition.Competition;
            manager.DeleteDirectory("Problems", competition.Notation, yearCompetition.Year.ToString(), prefix: Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            manager.DeleteDirectory("TestCases", competition.Notation, yearCompetition.Year.ToString());
            return RedirectToAction(nameof(Index));
        }

        private bool YearCompetitionExists(int id)
        {
            return manager.ReadAsync(id) != null;
        }
    }
}
