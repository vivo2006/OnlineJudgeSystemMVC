using System;
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

namespace OnlineJudgeSystemMVC.Controllers
{
    public class SubmissionsController : Controller
    {
        private readonly SubmissionsManager manager;
        private readonly ProblemsManager problemsManager;
        private readonly TestCaseResultContext testCaseResultManager;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly APIdata _apiData;


        public SubmissionsController(SubmissionsManager manager, ProblemsManager problemsManager, IHttpClientFactory httpClientFactory, TestCaseResultContext testCaseResultManager, UserManager<ApplicationUser> userManager, IOptions<APIdata> apiData)
        {
            this.manager = manager;
            this.problemsManager = problemsManager;
            this.httpClientFactory = httpClientFactory;
            this.testCaseResultManager = testCaseResultManager;
            this.userManager = userManager;
            _apiData = apiData.Value;
        }

        // GET: Submissions
        [Authorize(Roles ="Admin,User")]
        public async Task<IActionResult> Index(int problemId)
        {
            Problem problem = await problemsManager.ReadAsync(problemId, true);
            var user = await userManager.GetUserAsync(User);
            ViewBag.Problem = problem.Name;
            return View(await manager.ReadByIdAndProblemAsync(user.Id, problemId, true));
        }
        [Authorize(Roles = "Admin,User")]
        // GET: Submissions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var submission = await manager.ReadAsync((int)id, true);
            if (submission.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return View("AccessDenied");
            }
            if (submission == null)
            {
                return NotFound();
            }

            return View(submission);
        }
        [Authorize(Roles = "Admin,User")]

        // GET: Submissions/Create
        public IActionResult Create(int problemId)
        {
            ViewBag.ProblemId = problemId;
            return View();
        }
        [Authorize(Roles = "Admin,User")]

        public Tuple<string[], string[]> GetTestFiles(string competitionNotation, int year, string problemNotation)
        {
            // Construct the path to the test cases directory
            var testCasesInputPath = Path.Combine(Directory.GetCurrentDirectory(), "TestCases", competitionNotation, year.ToString(), problemNotation, "Input");
            var testCasesOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "TestCases", competitionNotation, year.ToString(), problemNotation, "Output");

            if (!Directory.Exists(testCasesInputPath) || !Directory.Exists(testCasesOutputPath))
            {
                throw new Exception("Test cases not found.");
            }

            // Read all test case files in the directory
            var inputTestFiles = Directory.GetFiles(testCasesInputPath);
            var outputTestFiles = Directory.GetFiles(testCasesOutputPath);

            return new Tuple<string[], string[]>(inputTestFiles, outputTestFiles);
        }
        [Authorize(Roles = "Admin,User")]

        public async Task<string> CreatePostQuery(string[] inputTestFiles, string[] outputTestFiles, int languageId, string sourceCode, double timeLimit, double memoryLimit)
        {
            BatchSubmissions batchRequest = new BatchSubmissions();
            int boundary = Math.Min(inputTestFiles.Length, 20);
            for (int i = 0; i < boundary; i++)
            {
                string inputContent = await System.IO.File.ReadAllTextAsync(inputTestFiles[i]);   // Read input file
                string outputContent = await System.IO.File.ReadAllTextAsync(outputTestFiles[i]); // Read expected output file

                batchRequest.submissions.Add(new PostRequest
                {
                    language_id = languageId,
                    source_code = sourceCode,
                    stdin = inputContent,
                    expected_output = outputContent,
                    cpu_time_limit = timeLimit,
                    memory_limit = memoryLimit
                });
            }
            // Convert to JSON
            var jsonSubmissionData = JsonConvert.SerializeObject(batchRequest, Formatting.Indented);
            System.Diagnostics.Debug.WriteLine(jsonSubmissionData);
            return jsonSubmissionData;
        }
        [Authorize(Roles = "Admin,User")]

        public async Task<List<PostResponse>> PostRequest(string jsonRequest, HttpClient client)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://judge0-ce.p.rapidapi.com/submissions/batch?base64_encoded=false&wait=false"),
                Headers =
                    {
                        { "x-rapidapi-key", _apiData.Key },
                        { "x-rapidapi-host", _apiData.Host },
                    },
                Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json"),
            };
            var jsonResponse = await client.SendAsync(request);
            var jsonContent = await jsonResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<PostResponse>>(jsonContent);
        }
        [Authorize(Roles = "Admin,User")]

        public async Task<BatchSubmissionResponse> GetRequest(List<PostResponse> tokens, HttpClient client)
        {
            List<string> tokenValues = tokens.Select(r => r.Token).ToList();
            string tokenString = string.Join(",", tokenValues);
            var getRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://judge0-ce.p.rapidapi.com/submissions/batch?tokens={tokenString}&base64_encoded=false&fields=stdout,status,time,memory"),
                Headers =
                {
                    { "x-rapidapi-key", _apiData.Key },
                    { "x-rapidapi-host", _apiData.Host },
                },
            };
            var jsonResponse = await client.SendAsync(getRequest);
            var jsonContent = await jsonResponse.Content.ReadAsStringAsync();
            // Deserialize the response
            return JsonSerializer.Deserialize<BatchSubmissionResponse>(jsonContent);
        }

        // POST: Submissions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Create(int ProblemId, string Code, int LanguageId, string CompetitionNotation, int Year, string ProblemNotation)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            BackgroundJob.Enqueue(() => ProcessSubmission(ProblemId, Code, LanguageId, CompetitionNotation, Year, ProblemNotation, userId));

            return RedirectToAction("Index", new {problemId = ProblemId});
        }
        [Authorize(Roles ="Admin,User")]
        public async Task ProcessSubmission(int ProblemId, string Code, int LanguageId, string CompetitionNotation, int Year, string ProblemNotation, string userId)
        {
            try
            {
                // Your submission processing logic (similar to the original method)

                Tuple<string[], string[]> testFiles = GetTestFiles(CompetitionNotation, Year, ProblemNotation);
                var inputTestFiles = testFiles.Item1;
                var outputTestFiles = testFiles.Item2;

                var problem = await problemsManager.ReadAsync(ProblemId, true);
                var groupIndex = (int)(Convert.ToChar(problem.Competition.Group) - 'A');
                var user = await userManager.FindByIdAsync(userId);
                Submission submission = new Submission(ProblemId, DateTime.Now, userId);

                await manager.CreateAsync(submission);

                string jsonRequest = await CreatePostQuery(inputTestFiles, outputTestFiles, LanguageId, Code, problem.TimeLimit, problem.MemoryLimit * 666);
                var client = httpClientFactory.CreateClient();
                List<PostResponse> tokens = await PostRequest(jsonRequest, client);
                await Task.Delay(10000); // Simulate delay

                BatchSubmissionResponse results = await GetRequest(tokens, client);

                // Handle test case results and update the database
                submission.TestCaseResults = new List<TestCaseResult>();
                var numberFormatInfo = new NumberFormatInfo { NumberDecimalSeparator = "." };
                foreach (var result in results.Submissions)
                {
                    TestCaseResult testCaseResult = new TestCaseResult(
                        submission.Id,
                        result.Status.description,
                        (int)Math.Round((decimal)((result.Memory ?? 0) / 1024.0)),
                        result.Stdout ?? "",
                        result.Time ?? "0.0"
                    );
                    submission.TestCaseResults.Add(testCaseResult);
                    await testCaseResultManager.CreateAsync(testCaseResult);
                }

                // Additional user and submission updates
                submission.Time = submission.TestCaseResults.Max(r => double.Parse(r.Time, numberFormatInfo));
                submission.Memory = submission.TestCaseResults.Max(r => Convert.ToInt32(r.Memory));
                submission.Error = submission.TestCaseResults.FirstOrDefault(r => r.State != "Accepted")?.State;
                submission.Points = (int)Math.Round(((decimal)submission.TestCaseResults.Count(r => r.State == "Accepted") / (decimal)submission.TestCaseResults.Count()) * (decimal)100.0);
                submission.Code = Code;

                await userManager.UpdateAsync(user);
                await manager.UpdateAsync(submission, true);
            }
            catch (Exception ex)
            {
                // Log errors to ensure visibility
                Console.WriteLine($"Error processing submission: {ex.Message}");
            }
        }

        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> OutputDetails(int id)
        {
            TestCaseResult testCaseResult = await testCaseResultManager.ReadAsync(id, true);
            Submission submission = await manager.ReadAsync(testCaseResult.SubmissionId, true);
            if (submission.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return View("AccessDenied");
            }
            return View("OutputDetails", testCaseResult.Output);
        }
        [Authorize(Roles = "Admin,User")]

        // GET: Submissions/Edit/5
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var submission = await manager.ReadAsync((int)id);
            if (submission == null)
            {
                return NotFound();
            }
            return View(submission);
        }

        // POST: Submissions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Points,Created,Error,Time,Memory,ProblemId")] Submission submission)
        {
            if (id != submission.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await manager.UpdateAsync(submission);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubmissionExists(submission.Id))
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
            return View(submission);
        }

        // GET: Submissions/Delete/5
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var submission = await manager.ReadAsync((int)id);
            if (submission == null)
            {
                return NotFound();
            }

            return View(submission);
        }

        // POST: Submissions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await manager.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private bool SubmissionExists(int id)
        {
            return manager.ReadAsync(id) != null;
        }
    }
}
