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
    public class CompetitionsController : Controller
    {
        private readonly CompetitionsManager manager;
        private readonly YearCompetitionsManager yearCompetitionsManager;
        private readonly GroupCompetitionsManager groupCompetitionsManager;


        public CompetitionsController(CompetitionsManager manager, YearCompetitionsManager yearCompetitionsManager, GroupCompetitionsManager groupCompetitionsManager)
        {
            this.manager = manager;
            this.yearCompetitionsManager = yearCompetitionsManager;
            this.groupCompetitionsManager = groupCompetitionsManager;
        }

        // GET: Competitions
        public async Task<IActionResult> Index()
        {
            return View(await manager.ReadAllAsync());
        }
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> AdministratorIndex()
        {
            return View(await manager.ReadAllAsync());
        }

        [Route("api/competitions/{id}/years")]
        [HttpGet]
        public async Task<IActionResult> GetYears(int id)
        {
            Competition competition = await manager.ReadAsync(id, true);
            List<Tuple<int, int>> yearId = competition.YearCompetitions.Select(x => new Tuple<int, int> (x.Id, x.Year)).ToList();
            return Ok(yearId);
        }
        public async Task<IActionResult> IndexYears(int id)
        {
            var competition = await manager.ReadAsync(id, true);
            if (competition == null)
            {
                return NotFound();
            }
            return View(competition.YearCompetitions);
        }

        // GET: Competitions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var competition = await manager.ReadAsync((int)id);
            if (competition == null)
            {
                return NotFound();
            }

            return View(competition);
        }
        [Authorize(Roles = "Admin")]

        // GET: Competitions/Create
        public IActionResult Create()
        {
            return View();
        }
        [Authorize(Roles = "Admin")]
        // POST: Competitions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,FirstYear,LastYear,Notation")]Competition competition)
        {
            
            if (ModelState.IsValid)
            {
                await manager.CreateAsync(competition);
                List<YearCompetition> yearCompetitions = new List<YearCompetition>();
                for (int i = competition.FirstYear; i <= competition.LastYear; i++)
                {
                    YearCompetition yearCompetition = new YearCompetition();
                    yearCompetition.Year = i;
                    yearCompetition.CompetitionId = competition.Id;                   
                    await yearCompetitionsManager.CreateAsync(yearCompetition);

                    List<GroupCompetition> groupCompetitions = new List<GroupCompetition>();
                    for(int j = 0; j < 5; j ++)
                    {
                        var groupCompetition = new GroupCompetition(((char)(j + 'A')).ToString(), yearCompetition.Id);
                        await groupCompetitionsManager.CreateAsync(groupCompetition);
                        groupCompetitions.Add(groupCompetition);
                    }
                    yearCompetition.GroupCompetitions = groupCompetitions;
                    await yearCompetitionsManager.UpdateAsync(yearCompetition);
                    yearCompetitions.Add(yearCompetition);
                }
                competition.YearCompetitions = yearCompetitions;
                await manager.UpdateAsync(competition, true);
                return RedirectToAction(nameof(AdministratorIndex));
            }
            return View(competition);
        }
        [Authorize(Roles = "Admin")]

        // GET: Competitions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var competition = await manager.ReadAsync((int)id);
            if (competition == null)
            {
                return NotFound();
            }
            return View(competition);
        }
        [Authorize(Roles = "Admin")]
        // POST: Competitions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Notation")] Competition competition)
        {
            if (id != competition.Id)
            {
                return BadRequest();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    await manager.UpdateAsync(competition);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompetitionExists(competition.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(AdministratorIndex));
            }
            return View(competition);
        }
        [Authorize(Roles = "Admin")]
        // GET: Competitions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var competition = await manager.ReadAsync((int)id);
            if (competition == null)
            {
                return NotFound();
            }

            return View(competition);
        }
        [Authorize(Roles = "Admin")]
        // POST: Competitions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var competition = await manager.ReadAsync(id);
            if (competition != null)
            {
                await manager.DeleteAsync(id);
                manager.DeleteDirectory("Problems", competition.Notation);
                manager.DeleteDirectory("TestCases", competition.Notation);
            }

            return RedirectToAction(nameof(AdministratorIndex));
        }

        private bool CompetitionExists(int id)
        {
            return manager.ReadAsync(id) != null;
        }
    }
}
