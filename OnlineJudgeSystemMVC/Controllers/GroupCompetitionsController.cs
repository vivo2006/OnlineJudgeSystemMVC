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
    public class GroupCompetitionsController : Controller
    {
        private readonly GroupCompetitionsManager manager;
        private readonly CompetitionsManager competitionsManager;

        public GroupCompetitionsController(GroupCompetitionsManager manager, CompetitionsManager competitionsManager)
        {
            this.manager = manager;
            this.competitionsManager = competitionsManager;
        }

        // GET: GroupCompetitions
        public async Task<IActionResult> Index()
        { 
            return View(await manager.ReadAllAsync());
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
            //ViewData["CompetitionId"] = new SelectList(await competitionsManager.ReadAllAsync(), "Id", "Name", groupCompetition.CompetitionId);
            return View(groupCompetition);
        }

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
