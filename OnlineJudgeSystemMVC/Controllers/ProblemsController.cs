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
using ServiceLayer.Storage;
using OnlineJudgeSystemMVC.Services;

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

        [HttpPost, ValidateAntiForgeryToken]
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

                var groupCompetition1 = await groupCompetitionsManager.ReadAsync(problem.CompetitionId, true);
                var yearCompetition1 = await yearCompetitionsManager.ReadAsync(groupCompetition1.YearCompetitionId, true);
                var competition1 = await competitionsManager.ReadAsync(yearCompetition1.CompetitionId, true);

                return RedirectToAction("Details", new { id = problem.Id, competitionNotation = competition1.Notation, year = yearCompetition1.Year });
            }

            var solvedTaskIds = (await submissionsManager.ReadAllAsync(true)).Where(s => s.UserId == user.Id && s.Points >= 100).Select(s => s.ProblemId).Distinct().ToList();

            if (!solvedTaskIds.Any())
            {
                return RedirectToAction("ChallengeMeInfo", "Home");
            }

            await UpdateChallenge(user, solvedTaskIds);

            var groupCompetition = await groupCompetitionsManager.ReadAsync(randomTask.CompetitionId, true);
            var yearCompetition = await yearCompetitionsManager.ReadAsync(groupCompetition.YearCompetitionId, true);
            var competition = await competitionsManager.ReadAsync(yearCompetition.CompetitionId, true);

            return RedirectToAction("Details", new {id = randomTask.Id, competitionNotation = competition.Notation, year = yearCompetition.Year });
        }

        private async Task UpdateChallenge(ApplicationUser user, List<int> solvedTaskIds)
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
                .Where(p => p.YearCompetitionId == groupCompetition.YearCompetitionId)
                .ToList();

            if (problems.Any(p => p.ProblemNotation == problemNotation))
            {
                ModelState.AddModelError("", "Нотацията трябва да е уникална.");
            }

            try
            {
                ProblemStorage.Segment(problemNotation);
                TestCaseUploads.Validate(testCases ?? Array.Empty<IFormFile>(), TestLimit);
                ValidatePdf(pdfFile);
            }
            catch (ArgumentException e) { ModelState.AddModelError("", e.Message); }
            var problem = new Problem(name, timeLimit, memoryLimit, problemNotation, groupId);
            problem.YearCompetitionId = groupCompetition.YearCompetitionId;
            await LoadCompetitions(groupId);

            if (ModelState.IsValid)
            {
                await UploadTask(pdfFile, problem);
                await problemsManager.CreateAsync(problem);
                await UploadFolder(testCases!, problem);

                return RedirectToAction(nameof(Index), "GroupCompetitions", new { competitionId = globalCompetition.Id, yearId = globalYearCompetition.Id });
            }

            return View();
        }

        private async Task<IActionResult> UploadFolder(IFormFile[] files, Problem problem)
        {
            if (files == null || files.Length == 0)
                throw new Exception("No files uploaded.");

            await LoadCompetitions(problem.CompetitionId);

            var uploadDirectory = ProblemStorage.DirectoryFor(Path.Combine(webHostEnvironment.ContentRootPath, "TestCases"), globalCompetition.Notation, globalYearCompetition.Year, problem.ProblemNotation);
            await TestCaseUploads.SaveAsync(uploadDirectory, files, TestLimit);
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
            ModelState.Remove("YearCompetition");
            problem.YearCompetitionId = globalGroupCompetition.YearCompetitionId;
            problem.URL = $"/Problems/{Uri.EscapeDataString(globalCompetition.Notation)}/{globalYearCompetition.Year}/{Uri.EscapeDataString(problem.ProblemNotation)}.pdf";
            ModelState.Remove("URL");

            try
            {
                ProblemStorage.Segment(problem.ProblemNotation);
                if (pdfFile != null) ValidatePdf(pdfFile);
                if (testCases?.Length > 0) TestCaseUploads.Validate(testCases, TestLimit);
            }
            catch (ArgumentException e) { ModelState.AddModelError("", e.Message); }
            if (!ModelState.IsValid) return View(problem);

            try
            {
                await problemsManager.UpdateAsync(problem);

                if (pdfFile != null)
                {
                    DeleteDirectories(problem);
                    await UploadTask(pdfFile, problem);
                    await problemsManager.UpdateAsync(problem);
                }

                if (testCases?.Length > 0)
                {
                    await UploadFolder(testCases!, problem);
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

        private int TestLimit => HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<BusinessLayer.API.JudgingOptions>>().Value.MaxTestCases;

        private static void ValidatePdf(IFormFile? file)
        {
            if (file == null || file.Length == 0 || file.Length > 10 * 1024 * 1024 || !Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Upload a PDF statement up to 10 MB.");
            using var input = file.OpenReadStream();
            Span<byte> header = stackalloc byte[5];
            if (input.Read(header) != 5 || !header.SequenceEqual("%PDF-"u8))
                throw new ArgumentException("The statement must contain a PDF document.");
        }

        private async Task UploadTask(IFormFile pdfFile, Problem problem)
        {
            ValidatePdf(pdfFile);
            var uploadDirectory = Path.Combine(webHostEnvironment.WebRootPath, "Problems", ProblemStorage.Segment(globalCompetition.Notation), globalYearCompetition.Year.ToString());
            Directory.CreateDirectory(uploadDirectory);

            var filePath = Path.Combine(uploadDirectory, $"{ProblemStorage.Segment(problem.ProblemNotation)}.pdf");

            problem.URL = $"/Problems/{Uri.EscapeDataString(globalCompetition.Notation)}/{globalYearCompetition.Year}/{Uri.EscapeDataString(problem.ProblemNotation)}.pdf";

            using var stream = new FileStream(filePath, FileMode.Create);
            await pdfFile.CopyToAsync(stream);
        }

        private void DeleteDirectories(Problem problem, bool testCasesFolder = false)
        {
            var folder = testCasesFolder ? "TestCases" : "Problems";
            problemsManager.DeleteDirectory(folder, globalCompetition.Notation, globalYearCompetition.Year.ToString(), problem.ProblemNotation, testCasesFolder ? webHostEnvironment.ContentRootPath : webHostEnvironment.WebRootPath);
        }
    }
}
