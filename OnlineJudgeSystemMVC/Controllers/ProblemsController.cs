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

namespace OnlineJudgeSystemMVC.Controllers
{
    public class ProblemsController : Controller
    {
        private readonly ProblemsManager manager;

        public ProblemsController(ProblemsManager manager)
        {
            this.manager = manager;
        }

        // GET: Problems
        public async Task<IActionResult> Index()
        {
            return View(await manager.ReadAllAsync(true));
        }

        // GET: Problems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var problem = await manager.ReadAsync((int)id);
            if (problem == null)
            {
                return NotFound();
            }

            return View(problem);
        }

        // GET: Problems/Create
        public IActionResult Create()
        {
            //ViewData["CompetitionId"] = new SelectList(_context.GroupCompetitions, "Id", "Group");
            return View();
        }

        // POST: Problems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,URL,CompetitionId")] Problem problem)
        {
            problem.Created = DateTime.Now;
            problem.LastModified = DateTime.Now;
            if (ModelState.IsValid)
            {
                await manager.CreateAsync(problem);
                return RedirectToAction(nameof(Index));
            }
            //ViewData["CompetitionId"] = new SelectList(_context.GroupCompetitions, "Id", "Group", problem.CompetitionId);
            return View(problem);
        }

        // GET: Problems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var problem = await manager.ReadAsync((int)id);
            if (problem == null)
            {
                return NotFound();
            }
            //ViewData["CompetitionId"] = new SelectList(_context.GroupCompetitions, "Id", "Group", problem.CompetitionId);
            return View(problem);
        }

        // POST: Problems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,URL,CompetitionId")] Problem problem)
        {
            if (id != problem.Id)
            {
                return BadRequest();
            }
            problem.LastModified = DateTime.Now;
            problem.Created = (await manager.ReadAsync((int)id)).Created;
            if (ModelState.IsValid)
            {
                try
                {
                    await manager.UpdateAsync(problem);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProblemExists(problem.Id))
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
            //ViewData["CompetitionId"] = new SelectList(_context.GroupCompetitions, "Id", "Group", problem.CompetitionId);
            return View(problem);
        }

        // GET: Problems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var problem = await manager.ReadAsync((int)id);
            if (problem == null)
            {
                return NotFound();
            }

            return View(problem);
        }

        // POST: Problems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var problem = await manager.ReadAsync(id);
            if (problem != null)
            {
                await manager.DeleteAsync(id);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProblemExists(int id)
        {
            return manager.ReadAsync(id) != null;
        }
    }
}
