using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DataLayer;
using ServiceLayer.EntityServices;
using BusinessLayer.Entities;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class ProblemsController : Controller
    {
        private readonly ProblemsManager problemsManager;
        private readonly GroupCompetitionsManager groupCompetitionsManager;
        private readonly YearCompetitionsManager yearCompetitionsManager;
        private readonly CompetitionsManager competitionsManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IdentityManager identityManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SubmissionsManager submissionsManager;

        private GroupCompetition globalGroupCompetition;
        private YearCompetition globalYearCompetition;
        private Competition globalCompetition;
        private Problem randomTask;

        public ProblemsController(ProblemsManager problemsManager, GroupCompetitionsManager groupCompetitionsManager, YearCompetitionsManager yearCompetitionsManager, CompetitionsManager competitionsManager, IWebHostEnvironment webHostEnvironment, IdentityManager identityManager, UserManager<ApplicationUser> userManager, SubmissionsManager submissionsManager, IWebHostEnvironment env)
        {
            this.problemsManager = problemsManager;
            this.groupCompetitionsManager = groupCompetitionsManager;
            this.yearCompetitionsManager = yearCompetitionsManager;
            this.competitionsManager = competitionsManager;
            this.webHostEnvironment = webHostEnvironment;
            this.identityManager = identityManager;
            this.userManager = userManager;
            this.submissionsManager = submissionsManager;
        }

        // GET: Problems
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var problems = await problemsManager.ReadAllAsync(true);
            return View(problems);
        }

        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> ChallengeMe()
        {
            var userId = userManager.GetUserId(User);
            var user = await identityManager.ReadUserAsync(userId, true);

            if (user.LastChallengeDate.HasValue && user.LastChallengeDate.Value.Date == DateTime.UtcNow.Date)
            {
                var problem = await problemsManager.ReadAsync((int)user.LastChallengeTaskId);
                await LoadCompetitions(problem.CompetitionId);

                ViewBag.CompetitionNotation = globalCompetition.Notation;
                ViewBag.Year = globalYearCompetition.Year;
                ViewBag.ChallengeId = user.LastChallengeTaskId;

                return View("Details", problem);
            }

            var solvedTaskIds = (await submissionsManager.ReadAllAsync(true)).Where(s => s.UserId == user.Id && s.Points >= 100).Select(s => s.ProblemId).Distinct().ToList();

            if (!solvedTaskIds.Any())
            {
                return RedirectToAction("ChallengeMeInfo", "Home");
            }

            await UpdateChallenge(user, solvedTaskIds);

            return View("Details", randomTask);
        }

        public async Task UpdateChallenge(ApplicationUser user, List<int> solvedTaskIds)
        {
            var random = new Random();
            int randomTaskId = solvedTaskIds[random.Next(solvedTaskIds.Count)];

            user.LastChallengeDate = DateTime.UtcNow;
            user.LastChallengeTaskId = randomTaskId;

            randomTask = await problemsManager.ReadAsync(randomTaskId, true);

            user.BestTime = user.Submissions.Where(s => s.ProblemId == randomTaskId).Min(s => s.Time);
            user.BestMemory = user.Submissions.Where(s => s.ProblemId == randomTaskId).Min(s => s.Memory);

            await LoadCompetitions(randomTask.CompetitionId);

            ViewBag.CompetitionNotation = globalCompetition.Notation;
            ViewBag.Year = globalYearCompetition.Year;
            ViewBag.ChallengeId = user.LastChallengeTaskId;

            await identityManager.UpdateUserAsync(user);
        }

        // GET: Problems/Details/5
        public async Task<IActionResult> Details(int? id, string competitionNotation, int year)
        {
            if (id == null)
                return BadRequest();

            var problem = await problemsManager.ReadAsync(id.Value);
            if (problem == null)
                return NotFound();

            ViewBag.CompetitionNotation = competitionNotation;
            ViewBag.Year = year;

            var userId = userManager.GetUserId(User);
            if (userId != null)
            {
                var user = await identityManager.ReadUserAsync(userId, true);
                ViewBag.ChallengeId = user.LastChallengeTaskId;
            }

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(string name, int groupId, double timeLimit, int memoryLimit, string problemNotation, IFormFile pdfFile, IFormFile[] testCases)
        {
            await LoadViewBag(groupId);

            var groupCompetition = await groupCompetitionsManager.ReadAsync(groupId, true);
            var problems = (await problemsManager.ReadAllAsync(true))
                .Where(p => p.Competition.YearCompetitionId == groupCompetition.YearCompetitionId)
                .ToList();

            if (problems.Any(p => p.ProblemNotation == problemNotation))
            {
                ModelState.AddModelError("", "Нотацията трябва да е уникална.");
            }

            var problem = new Problem(name, timeLimit, memoryLimit, problemNotation, groupId);
            await LoadCompetitions(groupId);

            if (ModelState.IsValid)
            {
                await UploadTask(pdfFile, problem);
                await problemsManager.CreateAsync(problem);
                await UploadFolder(testCases, problem);

                return RedirectToAction(nameof(Index), "GroupCompetitions", new { competitionId = globalCompetition.Id, yearId = globalYearCompetition.Id });
            }

            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadFolder(IFormFile[] files, Problem problem)
        {
            if (files == null || files.Length == 0)
                throw new Exception("No files uploaded.");

            await LoadCompetitions(problem.CompetitionId);

            var uploadDirectory = Path.Combine(webHostEnvironment.WebRootPath, "TestCases", globalCompetition.Notation, globalYearCompetition.Year.ToString(), problem.ProblemNotation);
            var inputFolderPath = Path.Combine(uploadDirectory, "Input");
            var outputFolderPath = Path.Combine(uploadDirectory, "Output");

            Directory.CreateDirectory(inputFolderPath);
            Directory.CreateDirectory(outputFolderPath);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file.FileName);
                var fileExtension = Path.GetExtension(fileName).ToLower();

                var filePath = fileExtension switch
                {
                    ".in" => Path.Combine(inputFolderPath, fileName),
                    ".sol" or ".out" => Path.Combine(outputFolderPath, fileName),
                    _ => null
                };

                if (filePath != null)
                {
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
            }

            return RedirectToAction(nameof(Index), "GroupCompetitions", new { competitionId = globalCompetition.Id, yearId = globalYearCompetition.Id });
        }

        // GET: Problems/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id, int groupId)
        {
            if (id == null)
                return BadRequest();

            var problem = await problemsManager.ReadAsync(id.Value);
            if (problem == null)
                return NotFound();

            await LoadViewBag(groupId);

            return View(problem);
        }

        // POST: Problems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,URL,TimeLimit,MemoryLimit,CompetitionId,ProblemNotation")] Problem problem, IFormFile? pdfFile, IFormFile[]? testCases)
        {
            await LoadCompetitions(problem.CompetitionId);
            ModelState.Remove("Competition");

            if (!ModelState.IsValid)
                return View(problem);

            try
            {
                await problemsManager.UpdateAsync(problem);

                if (pdfFile != null)
                {
                    DeleteDirectories(problem);
                    await UploadTask(pdfFile, problem);
                }

                if (testCases?.Length > 0)
                {
                    DeleteDirectories(problem, testCasesFolder: true);
                    await UploadFolder(testCases, problem);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ProblemExists(problem.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index), "GroupCompetitions", new { competitionId = globalCompetition.Id, yearId = globalYearCompetition.Id });
        }

        // GET: Problems/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return BadRequest();

            var problem = await problemsManager.ReadAsync(id.Value);
            if (problem == null)
                return NotFound();

            return View(problem);
        }

        // POST: Problems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var problem = await problemsManager.ReadAsync(id);
            if (problem != null)
            {
                await LoadCompetitions(problem.CompetitionId);

                DeleteDirectories(problem);
                DeleteDirectories(problem, testCasesFolder: true);

                await problemsManager.DeleteAsync(id);
            }

            return RedirectToAction(nameof(Index), "GroupCompetitions", new { competitionId = globalCompetition.Id, yearId = globalYearCompetition.Id });
        }

        private async Task<bool> ProblemExists(int id)
        {
            return (await problemsManager.ReadAsync(id)) != null;
        }

        private async Task LoadViewBag(int id)
        {
            var groupCompetition = await groupCompetitionsManager.ReadAsync(id, true);
            var yearCompetition = groupCompetition.YearCompetition;
            var competition = await competitionsManager.ReadAsync(yearCompetition.CompetitionId);

            ViewBag.GroupId = id;
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

        private async Task UploadTask(IFormFile pdfFile, Problem problem)
        {
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Problems", globalCompetition.Notation, globalYearCompetition.Year.ToString());
            Directory.CreateDirectory(uploadDirectory);

            var filePath = Path.Combine(uploadDirectory, $"{problem.ProblemNotation}.pdf");
            var port = HttpContext.Request.Host.Port?.ToString() ?? "80";
            var localhost = $"https://localhost:{port}";

            problem.URL = $"{localhost}/Problems/{globalCompetition.Notation}/{globalYearCompetition.Year}/{problem.ProblemNotation}.pdf";

            using var stream = new FileStream(filePath, FileMode.Create);
            await pdfFile.CopyToAsync(stream);
        }

        private void DeleteDirectories(Problem problem, bool testCasesFolder = false)
        {
            var folder = testCasesFolder ? "TestCases" : "Problems";
            problemsManager.DeleteDirectory(folder, globalCompetition.Notation, globalYearCompetition.Year.ToString(), problem.ProblemNotation, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
        }
    }
}
