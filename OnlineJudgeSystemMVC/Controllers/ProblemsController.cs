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
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class ProblemsController : Controller
    {
        private readonly ProblemsManager manager;
        private readonly GroupCompetitionsManager groupCompetitionsManager;
        private readonly YearCompetitionsManager yearCompetitionsManager;
        private readonly CompetitionsManager competitionsManager;
        private readonly IWebHostEnvironment environment;

        GroupCompetition globalGroupCompetition;
        YearCompetition globalYearCompetition;
        Competition globalCompetition;



        public ProblemsController(ProblemsManager manager, GroupCompetitionsManager groupCompetitionsManager, YearCompetitionsManager yearCompetitionsManager, CompetitionsManager competitionsManager, IWebHostEnvironment environment)
        {
            this.manager = manager;
            this.groupCompetitionsManager = groupCompetitionsManager;
            this.yearCompetitionsManager = yearCompetitionsManager;
            this.competitionsManager = competitionsManager;
            this.environment = environment;
        }

        // GET: Problems
        public async Task<IActionResult> Index()
        {
            return View(await manager.ReadAllAsync(true));
        }

        // GET: Problems/Details/5
        public async Task<IActionResult> Details(int? id, string CompetitionNotation, int Year)
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

            ViewBag.CompetitionNotation = CompetitionNotation;
            ViewBag.Year = Year;

            return View(problem);
        }

        // GET: Problems/Create
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Create(int id)
        {
            await LoadViewBag(id);
            return View();
        }

        // POST: Problems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Create(string name, int groupId, double timeLimit, int memoryLimit, string problemNotation, IFormFile pdfFile, IFormFile[] testCases)
        {
            Problem problem = new Problem(name, timeLimit, memoryLimit, problemNotation, groupId);
            await LoadCompetitions(groupId);
            
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Problems", globalCompetition.Notation, globalYearCompetition.Year.ToString());

            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            string filePath = Path.Combine(uploadDirectory, problemNotation + ".pdf");
            string port = HttpContext.Request.Host.Port.ToString() ?? "80"; 
            string localhost = $"https://localhost:{port}";

            string newUrl = $"{localhost}/Problems/{globalCompetition.Notation}/{globalYearCompetition.Year.ToString()}/{problemNotation}.pdf";
            problem.URL = newUrl;

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await pdfFile.CopyToAsync(stream);
            }
            if (ModelState.IsValid)
            {
                await manager.CreateAsync(problem);            
                await UploadFolder(testCases, problem);

            }
            return Redirect($"/GroupCompetitions/Index/?competitionId={globalCompetition.Id}&yearId={globalYearCompetition.Id}");
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadFolder(IFormFile[] files, Problem problem)
        {
            if (files == null || files.Length == 0)
            {
                throw new Exception("No files uploaded.");
            }

            await LoadCompetitions(problem.CompetitionId);

            var uploadFolder = Path.Combine(environment.ContentRootPath, "TestCases", globalCompetition.Notation, globalYearCompetition.Year.ToString(), problem.ProblemNotation);
            var inputFolderPath = Path.Combine(uploadFolder, "Input");
            var outputFolderPath = Path.Combine(uploadFolder, "Output");

            if (!Directory.Exists(inputFolderPath))
            {
                Directory.CreateDirectory(inputFolderPath);
            }
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file.FileName);
                var fileExtension = Path.GetExtension(fileName).ToLower();

                if (fileExtension == ".in" && file.Length > 0)
                {
                    var inputFilePath = Path.Combine(inputFolderPath, fileName);
                    using (var stream = new FileStream(inputFilePath, FileMode.Create))
                    {
                         file.CopyTo(stream);
                    }
                }
                else if ((fileExtension == ".sol" || fileExtension == ".out") && file.Length > 0)
                {
                    var outputFilePath = Path.Combine(outputFolderPath, fileName); 
                    using (var stream = new FileStream(outputFilePath, FileMode.Create))
                    {
                         file.CopyTo(stream);
                    }
                }
                else
                {
                    continue;
                }
            }

            return Redirect($"/GroupCompetitions/Index/?competitionId={globalCompetition.Id} &yearId= {globalYearCompetition.Id}");
        }

        [Authorize(Roles = "Admin")]
        // GET: Problems/Edit/5
        public async Task<IActionResult> Edit(int? id, int groupId)
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
            await LoadViewBag(groupId);
            
            return View(problem);
        }

        // POST: Problems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,GroupId,TimeLimit,MemoryLimit,ProblemNotation")] Problem problem, IFormFile pdfFile, IFormFile[] testCases)
        {
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
                return Redirect($"/GroupCompetitions/Index/?competitionId={globalCompetition.Id}&yearId={globalYearCompetition.Id}");
            }
            return View(problem);
        }
        [Authorize(Roles = "Admin")]

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
        [Authorize(Roles = "Admin")]

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
        private async Task LoadViewBag(int id)
        {
            ViewBag.GroupId = id;
            GroupCompetition groupCompetition = await groupCompetitionsManager.ReadAsync(id, true);
            YearCompetition yearCompetition = groupCompetition.YearCompetition;
            Competition competition = await competitionsManager.ReadAsync(yearCompetition.CompetitionId);
            ViewBag.Year = yearCompetition.Year;
            ViewBag.Competition = competition.Name;
            ViewBag.Group = groupCompetition.Group;
            ViewBag.CompetitionId = competition.Id;
            ViewBag.YearId = yearCompetition.Id;
        }
        private async Task LoadCompetitions(int groupId)
        {
            globalGroupCompetition = await groupCompetitionsManager.ReadAsync(groupId, true);
            globalYearCompetition = await yearCompetitionsManager.ReadAsync(globalGroupCompetition.YearCompetitionId, true);
            globalCompetition = await competitionsManager.ReadAsync(globalYearCompetition.CompetitionId, true);
        }
    }
}
