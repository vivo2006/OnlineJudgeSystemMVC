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

namespace OnlineJudgeSystemMVC.Controllers
{
    public class ProblemsController : Controller
    {
        private readonly ProblemsManager manager;
        private readonly GroupCompetitionsManager groupCompetitionsManager;
        private readonly YearCompetitionsManager yearCompetitionsManager;
        private readonly CompetitionsManager competitionsManager;
        private readonly IWebHostEnvironment environment;


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
        public async Task<IActionResult> Create(int id)
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
            return View();
        }

        // POST: Problems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, int groupId, double timeLimit, int memoryLimit, string problemNotation, IFormFile pdfFile, IFormFile[] testCases)
        {
            Problem problem = new Problem();
            problem.Name = name;
            problem.TimeLimit = timeLimit;
            problem.MemoryLimit = memoryLimit;
            problem.ProblemNotation = problemNotation;
            problem.CompetitionId = groupId;

            GroupCompetition groupCompetition = await groupCompetitionsManager.ReadAsync(groupId, true);
            YearCompetition yearCompetition = await yearCompetitionsManager.ReadAsync(groupCompetition.YearCompetitionId, true);
            Competition competition = await competitionsManager.ReadAsync(yearCompetition.CompetitionId, true);
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Problems", competition.Notation, yearCompetition.Year.ToString());

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
            string newUrl = $"{localhost}/Problems/{competition.Notation}/{yearCompetition.Year.ToString()}/{problemNotation}.pdf";
            problem.URL = newUrl;

            // Save the PDF file to the specified path
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await pdfFile.CopyToAsync(stream);
            }
            if (ModelState.IsValid)
            {
                await manager.CreateAsync(problem);            
                await UploadFolder(testCases, problem);

            }
            return Redirect($"/GroupCompetitions/Index/?competitionId={competition.Id}&yearId={yearCompetition.Id}");
        }
        public async Task<IActionResult> UploadFolder(IFormFile[] files, Problem problem)
        {
            if (files == null || files.Length == 0)
            {
                throw new Exception("No files uploaded.");
            }

            int groupId = problem.CompetitionId;

            // Fetch necessary entities
            GroupCompetition groupCompetition = await groupCompetitionsManager.ReadAsync(groupId, true);
            YearCompetition yearCompetition = await yearCompetitionsManager.ReadAsync(groupCompetition.YearCompetitionId, true);
            Competition competition = await competitionsManager.ReadAsync(yearCompetition.CompetitionId, true);

            // Define folder paths
            var uploadFolder = Path.Combine(environment.ContentRootPath, "TestCases", competition.Notation, yearCompetition.Year.ToString(), problem.ProblemNotation);
            var inputFolderPath = Path.Combine(uploadFolder, "Input");
            var outputFolderPath = Path.Combine(uploadFolder, "Output");

            // Create directories if they don't exist
            if (!Directory.Exists(inputFolderPath))
            {
                Directory.CreateDirectory(inputFolderPath);
            }
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            // Save files
            foreach (var file in files)
            {
                // Ensure the file name is correct
                var fileName = Path.GetFileName(file.FileName);
                var fileExtension = Path.GetExtension(fileName).ToLower();

                if (fileExtension == ".in" && file.Length > 0)
                {
                    var inputFilePath = Path.Combine(inputFolderPath, fileName); // Save in Input folder
                    using (var stream = new FileStream(inputFilePath, FileMode.Create))
                    {
                         file.CopyTo(stream);
                    }
                }
                else if (fileExtension == ".sol" && file.Length > 0)
                {
                    var outputFilePath = Path.Combine(outputFolderPath, fileName); // Save in Output folder
                    using (var stream = new FileStream(outputFilePath, FileMode.Create))
                    {
                         file.CopyTo(stream);
                    }
                }
                else
                {
                    // Skip unsupported files
                    continue;
                }
            }

            // Redirect to another page
            return Redirect($"/GroupCompetitions/Index/?competitionId={competition.Id}&yearId={yearCompetition.Id}");
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
