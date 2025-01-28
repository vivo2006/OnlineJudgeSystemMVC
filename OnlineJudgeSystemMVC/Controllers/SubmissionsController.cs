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
using System.Text;
using System.Text.Json;
using Azure;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class SubmissionsController : Controller
    {
        private readonly SubmissionsManager manager;
        private readonly ProblemsManager problemsManager;
        private readonly IHttpClientFactory httpClientFactory;


        public SubmissionsController(SubmissionsManager manager, ProblemsManager problemsManager, IHttpClientFactory httpClientFactory)
        {
            this.manager = manager;
            this.problemsManager = problemsManager;
            this.httpClientFactory = httpClientFactory;
        }

        // GET: Submissions
        public async Task<IActionResult> Index()
        {
            return View(await manager.ReadAllAsync());
        }

        // GET: Submissions/Details/5
        public async Task<IActionResult> Details(int? id)
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

        // GET: Submissions/Create
        public IActionResult Create(int problemId)
        {
            ViewBag.ProblemId = problemId;
            return View();
        }

        // POST: Submissions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(int ProblemId, string Code, int LanguageId, string CompetitionNotation, int Year, string ProblemNotation)
        {
            // Construct the path to the test cases directory
            var testCasesInputPath = Path.Combine(Directory.GetCurrentDirectory(), "TestCases", CompetitionNotation, Year.ToString(),ProblemNotation, "Input");
            var testCasesOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "TestCases", CompetitionNotation, Year.ToString(), ProblemNotation, "Output");

            if (!Directory.Exists(testCasesInputPath))
            {
                return NotFound("Test cases not found.");
            }

            // Read all test case files in the directory
            var inputTestFiles = Directory.GetFiles(testCasesInputPath);
            var outputTestFiles = Directory.GetFiles(testCasesOutputPath);
            var problem = await problemsManager.ReadAsync(ProblemId, true);

            Submission submission = new Submission(ProblemId);
            submission.Created = DateTime.Now;
            await manager.CreateAsync(submission);

            for (int i = 0; i < inputTestFiles.Length; i ++)
            {
                // Read the input for the current test case
                var testInput = await System.IO.File.ReadAllTextAsync(inputTestFiles[i]);
                var testOutput = await System.IO.File.ReadAllTextAsync(outputTestFiles[i]);

                // Prepare the submission data for Judge0
                var submissionData = new
                {
                    source_code = Code,
                    language_id = LanguageId,
                    stdin = testInput,
                    expected_output = testOutput,
                    cpu_time_limit = problem.TimeLimit,
                    memory_limit = problem.MemoryLimit * 1024
                };

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://judge0-ce.p.rapidapi.com/submissions?base64_encoded=false&wait=false"),
                    Headers =
                    {
                        { "x-rapidapi-key", "7a54853191msh7f87c61bdfeb479p145b68jsna8f13c73dbc8" },
                        { "x-rapidapi-host", "judge0-ce.p.rapidapi.com" },
                    },
                    Content = new StringContent(JsonSerializer.Serialize(submissionData), Encoding.UTF8, "application/json")
                };                               
                var client = httpClientFactory.CreateClient();
                var tokenResponse = await client.SendAsync(request);
                var body = await tokenResponse.Content.ReadAsStringAsync();
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error: {tokenResponse.StatusCode}");
                    Console.WriteLine($"Response Body: {body}");
                }


                // Send the request to Judge0 API
                string token = (JsonSerializer.Deserialize<PostResponse>(await tokenResponse.Content.ReadAsStringAsync())).token;
                var getRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://judge0-ce.p.rapidapi.com/submissions/{token}?base64_encoded=false&fields=stdout,status,time,memory"),
                    Headers =
                    {
                        { "x-rapidapi-key", "7a54853191msh7f87c61bdfeb479p145b68jsna8f13c73dbc8" },
                        { "x-rapidapi-host", "judge0-ce.p.rapidapi.com" },
                    },
                };
                var getSubmission = await client.SendAsync(getRequest);
                var bodySub = getSubmission.Content.ReadAsStringAsync();
                // Deserialize the response
                var result = JsonSerializer.Deserialize<Judge0Result>(await getSubmission.Content.ReadAsStringAsync());
                

                // Add the result to the results list
                TestCaseResult testCaseResult = new TestCaseResult(submission.Id, result.status.description, result.memory, result.stdout, result.time);
                submission.TestCaseResults = new List<TestCaseResult>();
                submission.TestCaseResults.Add(testCaseResult);
            }
            submission.Points = submission.TestCaseResults.Count(r => r.State == "Accepted");
            submission.Time = 1;
            submission.Error = submission.TestCaseResults.FirstOrDefault(r => r.State != "Accepted")?.State;
            submission.Memory = submission.TestCaseResults.Max(r => Convert.ToInt32(Math.Ceiling(r.Memory / 1024.0)));
            await manager.UpdateAsync(submission);
            // Pass the results to the Results view
            return RedirectToAction("Details", submission.TestCaseResults);
        }

        // GET: Submissions/Edit/5
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
