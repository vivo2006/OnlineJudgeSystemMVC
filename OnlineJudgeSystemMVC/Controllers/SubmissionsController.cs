using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Azure;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using BusinessLayer.API;
using ServiceLayer.EntityServices;
using DataLayer.Contexts;
using BusinessLayer.Entities;
using Hangfire;
using ServiceLayer.ExternalServices;
using Microsoft.AspNetCore.Hosting;
using Humanizer;

namespace OnlineJudgeSystemMVC.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class SubmissionsController : Controller
    {
        private readonly SubmissionsManager _manager;
        private readonly ProblemsManager _problemsManager;
        private readonly TestCaseResultContext _testCaseResultManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly APIdata _apiData;
        private readonly Judge0Service _judge0Service;
        private readonly IWebHostEnvironment _env;


        public SubmissionsController(
            SubmissionsManager manager,
            ProblemsManager problemsManager,
            IHttpClientFactory httpClientFactory,
            TestCaseResultContext testCaseResultManager,
            UserManager<ApplicationUser> userManager,
            IOptions<APIdata> apiData,
            Judge0Service judge0Service,
            IWebHostEnvironment env)
        {
            _manager = manager;
            _problemsManager = problemsManager;
            _httpClientFactory = httpClientFactory;
            _testCaseResultManager = testCaseResultManager;
            _userManager = userManager;
            _apiData = apiData.Value;
            _judge0Service = judge0Service;
            _env = env;
        }

        public async Task<IActionResult> Index(int problemId)
        {
            var problem = await _problemsManager.ReadAsync(problemId, true);
            if (problem == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            ViewBag.Problem = problem.Name;

            return View(await _manager.ReadByIdAndProblemAsync(user.Id, problemId, true));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var submission = await _manager.ReadAsync(id.Value, true);
            if (submission == null) return NotFound();

            if (submission.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return View("AccessDenied");

            return View(submission);
        }

        public IActionResult Create(int problemId)
        {
            ViewBag.ProblemId = problemId;
            return View();
        }

        public Tuple<string[], string[]> GetTestFiles(string competitionNotation, int year, string problemNotation)
        {
            var baseDirectory = Path.Combine(_env.WebRootPath, "TestCases", competitionNotation, year.ToString(), problemNotation);
            var inputPath = Path.Combine(baseDirectory, "Input");
            var outputPath = Path.Combine(baseDirectory, "Output");

            if (!Directory.Exists(inputPath) || !Directory.Exists(outputPath))
                throw new Exception("Test cases not found.");

            var inputTestFiles = Directory.GetFiles(inputPath);
            var outputTestFiles = Directory.GetFiles(outputPath);

            return new(inputTestFiles, outputTestFiles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int problemId, string code, int languageId, string competitionNotation, int year, string problemNotation)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            BackgroundJob.Enqueue(() => ProcessSubmission(problemId, code, languageId, competitionNotation, year, problemNotation, userId));

            return RedirectToAction(nameof(Index), new { problemId });
        }

        [Authorize(Roles = "Admin,User")]
        public async Task ProcessSubmission(int ProblemId, string Code, int LanguageId, string CompetitionNotation, int Year, string ProblemNotation, string userId)
        {
            /*try
            {*/
                var (inputTestFiles, outputTestFiles) = GetTestFiles(CompetitionNotation, Year, ProblemNotation);

                var problem = await _problemsManager.ReadAsync(ProblemId, true);
                var user = await _userManager.FindByIdAsync(userId);
                var bestTime = user.Submissions
                .Where(s => s.ProblemId == ProblemId && s.UserId == userId)
                .Select(s => s.Time)
                .DefaultIfEmpty(0)
                .Max();
                var bestMemory = user.Submissions
                  .Where(s => s.ProblemId == ProblemId && s.UserId == userId)
                    .Select(s => s.Memory).DefaultIfEmpty(0)
                .Max();

                var submission = new Submission(ProblemId, DateTime.Now, userId);
                await _manager.CreateAsync(submission);

                string jsonRequest = await _judge0Service.CreatePostQueryAsync(
                    inputTestFiles, outputTestFiles, LanguageId, Code, problem.TimeLimit, problem.MemoryLimit * 1024);

                var tokens = await _judge0Service.SubmitBatchAsync(jsonRequest);

                await Task.Delay(10000); 

                var results = await _judge0Service.GetBatchResultsAsync(tokens);

                submission.TestCaseResults = new List<TestCaseResult>();
                var numberFormatInfo = new NumberFormatInfo { NumberDecimalSeparator = "." };

                foreach (var result in results.Submissions)
                {
                    var testCaseResult = new TestCaseResult(
                        submission.Id,
                        result.Status.description,
                        (int)Math.Round((decimal)((result.Memory ?? 0) / 1024.0)),
                        result.Stdout ?? "",
                        result.Time ?? "0.0"
                    );

                    submission.TestCaseResults.Add(testCaseResult);
                    await _testCaseResultManager.CreateAsync(testCaseResult);
                }

                submission.Time = submission.TestCaseResults.Max(r => double.Parse(r.Time, numberFormatInfo));
                submission.Memory = submission.TestCaseResults.Max(r => Convert.ToInt32(r.Memory));
                submission.Error = submission.TestCaseResults.FirstOrDefault(r => r.State != "Accepted")?.State;
                submission.Points = (int)Math.Round(((decimal)submission.TestCaseResults.Count(r => r.State == "Accepted") / (decimal)submission.TestCaseResults.Count()) * 100.0m);
                submission.Code = Code;

                
                int currentPoints = submission.Points;

                int ind = (int)(Convert.ToChar(problem.ProblemNotation.Substring(0, 1)) - 'A');
                user.Points[ind] += (currentPoints);
                if(submission.Time < bestTime && ProblemId == user.LastChallengeTaskId)
                {
                    user.Points[ind] += 10;
                }
                else if(submission.Memory < bestMemory && ProblemId == user.LastChallengeTaskId)
                {
                    user.Points[ind] += 10;
                }

                await _userManager.UpdateAsync(user);
                await _manager.UpdateAsync(submission, true);
            /*}
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing submission: {ex.Message}");
            }*/
        }

        public async Task<IActionResult> OutputDetails(int id)
        {
            var testCaseResult = await _testCaseResultManager.ReadAsync(id, true);
            var submission = await _manager.ReadAsync(testCaseResult.SubmissionId, true);

            if (submission == null || submission.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return View("AccessDenied");

            return View("OutputDetails", testCaseResult.Output);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();

            var submission = await _manager.ReadAsync(id.Value);
            if (submission == null) return NotFound();

            return View(submission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Points,Created,Error,Time,Memory,ProblemId")] Submission submission)
        {
            if (id != submission.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    await _manager.UpdateAsync(submission);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubmissionExists(submission.Id)) return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(submission);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();

            var submission = await _manager.ReadAsync(id.Value);
            if (submission == null) return NotFound();

            return View(submission);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _manager.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private bool SubmissionExists(int id) => _manager.ReadAsync(id) != null;
    }
}