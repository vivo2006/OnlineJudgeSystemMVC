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
    public class YearCompetitionsController : Controller
    {
        private readonly YearCompetitionsManager manager;

        public YearCompetitionsController(YearCompetitionsManager manager)
        {
            this.manager = manager;
        }

        // GET: YearCompetitions
        public async Task<IActionResult> Index()
        {
            return View(await manager.ReadAllAsync());
        }
        [Route("/Competitions/{competitionId}/{id}/index")]
        // GET: GroupCompetitions from YearCompetition
        public async Task<IActionResult> IndexFromYearCompetition(int competitionId, int id)
        {
            YearCompetition yearCompetition = await manager.ReadAsync(id, true);
            List<GroupCompetition> groupCompetitions = (List<GroupCompetition>)yearCompetition.GroupCompetitions;
            return View(groupCompetitions);
        }

        // GET: YearCompetitions/Details/5
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
        public async Task<IActionResult> Delete(int? id)
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

        // POST: YearCompetitions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await manager.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private bool YearCompetitionExists(int id)
        {
            return manager.ReadAsync(id) != null;
        }
    }
}
