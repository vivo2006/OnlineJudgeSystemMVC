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
        private readonly GroupCompetitionsManager groupCompetitionsManager;
        private readonly YearCompetitionsManager yearCompetitionsManager;
        private readonly CompetitionsManager competitionsManager ;

        public ProblemsController(ProblemsManager manager, GroupCompetitionsManager groupCompetitionsManager, YearCompetitionsManager yearCompetitionsManager, CompetitionsManager competitionsManager)
        {
            this.manager = manager;
            this.groupCompetitionsManager = groupCompetitionsManager;
            this.yearCompetitionsManager = yearCompetitionsManager;
            this.competitionsManager = competitionsManager;
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
        public IActionResult CreateAsync(int GroupId)
        {
            ViewBag.GroupId = GroupId;
            return View();
        }

        // POST: Problems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, int groupId, double timeLimit, int memoryLimit, string problemNotation, IFormFile pdfFile)
        {
            Problem problem = new Problem();
            problem.Name = name;
            problem.TimeLimit = timeLimit;
            problem.MemoryLimit = memoryLimit;
            problem.ProblemNotation = problemNotation;
            problem.CompetitionId = groupId;

            GroupCompetition groupCompetition = await groupCompetitionsManager.ReadAsync(groupId, true);
            YearCompetition competition = await yearCompetitionsManager.ReadAsync(groupCompetition.YearCompetitionId, true);
            Competition comp = await competitionsManager.ReadAsync(competition.CompetitionId, true);
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Problems", comp.CompetitionAnnotation, competition.Year.ToString());

            // Check if the directory exists, and create it if it doesn't
            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            // Define the full path for the uploaded file
            string filePath = Path.Combine(uploadDirectory, problemNotation + ".pdf");
            string port = HttpContext.Request.Host.Port.ToString() ?? "80"; // Replace with your actual port
            string localhost = $"https://localhost:{port}";

            // Construct the new URL
            string newUrl = $"{localhost}/Problems/{comp.CompetitionAnnotation}/{competition.Year.ToString()}/{problemNotation}.pdf";
            problem.URL = newUrl;


            // Save the PDF file to the specified path
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await pdfFile.CopyToAsync(stream);
            }
            if (ModelState.IsValid)
            {
                await manager.CreateAsync(problem);
                return RedirectToAction(nameof(Index));
            }
            

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
            //ViewData["CompetitionId"] = new SelectList(await groupCompetitionsManager.ReadAllAsync(), "Id", "Group", problem.CompetitionId);
            
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
