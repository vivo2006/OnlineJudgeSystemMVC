using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BusinessLayer;
using DataLayer;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class YearCompetitionsController : Controller
    {
        private readonly OnlineJudgeDbContext _context;

        public YearCompetitionsController(OnlineJudgeDbContext context)
        {
            _context = context;
        }

        // GET: YearCompetitions
        public async Task<IActionResult> Index()
        {
            var onlineJudgeDbContext = _context.YearCompetitions.Include(y => y.Competition);
            return View(await onlineJudgeDbContext.ToListAsync());
        }

        // GET: YearCompetitions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yearCompetition = await _context.YearCompetitions
                .Include(y => y.Competition)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (yearCompetition == null)
            {
                return NotFound();
            }

            return View(yearCompetition);
        }

        // GET: YearCompetitions/Create
        public IActionResult Create()
        {
            ViewData["CompetitionId"] = new SelectList(_context.Competitions, "Id", "Name");
            return View();
        }

        // POST: YearCompetitions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Year,Created,LastModified,CompetitionId")] YearCompetition yearCompetition)
        {
            if (ModelState.IsValid)
            {
                _context.Add(yearCompetition);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CompetitionId"] = new SelectList(_context.Competitions, "Id", "Name", yearCompetition.CompetitionId);
            return View(yearCompetition);
        }

        // GET: YearCompetitions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yearCompetition = await _context.YearCompetitions.FindAsync(id);
            if (yearCompetition == null)
            {
                return NotFound();
            }
            ViewData["CompetitionId"] = new SelectList(_context.Competitions, "Id", "Name", yearCompetition.CompetitionId);
            return View(yearCompetition);
        }

        // POST: YearCompetitions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Year,Created,LastModified,CompetitionId")] YearCompetition yearCompetition)
        {
            if (id != yearCompetition.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(yearCompetition);
                    await _context.SaveChangesAsync();
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
            ViewData["CompetitionId"] = new SelectList(_context.Competitions, "Id", "Name", yearCompetition.CompetitionId);
            return View(yearCompetition);
        }

        // GET: YearCompetitions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yearCompetition = await _context.YearCompetitions
                .Include(y => y.Competition)
                .FirstOrDefaultAsync(m => m.Id == id);
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
            var yearCompetition = await _context.YearCompetitions.FindAsync(id);
            if (yearCompetition != null)
            {
                _context.YearCompetitions.Remove(yearCompetition);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool YearCompetitionExists(int id)
        {
            return _context.YearCompetitions.Any(e => e.Id == id);
        }
    }
}
