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
using System.Net.Http.Headers;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class SubmissionsController : Controller
    {
        private readonly SubmissionsManager manager;
        private readonly ProblemsManager problemsManager;
        private readonly TestCaseResultContext testCaseResultManager;
        private readonly IHttpClientFactory httpClientFactory;


        public SubmissionsController(SubmissionsManager manager, ProblemsManager problemsManager, IHttpClientFactory httpClientFactory, TestCaseResultContext testCaseResultManager)
        {
            this.manager = manager;
            this.problemsManager = problemsManager;
            this.httpClientFactory = httpClientFactory;
            this.testCaseResultManager = testCaseResultManager;
        }

        // GET: Submissions
        public async Task<IActionResult> Index()
        {
            return View(await manager.ReadAllAsync(true));
        }

        // GET: Submissions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var submission = await manager.ReadAsync((int)id, true);
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
        public async Task<string> CreatePostQuery(string[] inputTestFiles, string[] outputTestFiles, int languageId, string sourceCode, double timeLimit, double memoryLimit)
        {
            BatchSubmissions batchRequest = new BatchSubmissions();
            for (int i = 0; i < inputTestFiles.Length; i++)
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

        public async Task<List<PostResponse>> PostRequest(string jsonRequest, HttpClient client)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://judge0-ce.p.rapidapi.com/submissions/batch?base64_encoded=false&wait=false"),
                Headers =
                    {
                        { "x-rapidapi-key", "fe05a54966msh74f99cf84607244p1d78dfjsn7624b874c93f" },
                        { "x-rapidapi-host", "judge0-ce.p.rapidapi.com" },
                    },
                Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json"),
            };
            var jsonResponse = await client.SendAsync(request);
            var jsonContent = await jsonResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<PostResponse>>(jsonContent);
        }

        public async Task<BatchSubmissionResponse> GetRequest(List<PostResponse> tokens, HttpClient client)
        {
            List<string> tokenValues = tokens.Select(r => r.token).ToList();
            string tokenString = string.Join(",", tokenValues);
            var getRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://judge0-ce.p.rapidapi.com/submissions/batch?tokens={tokenString}&base64_encoded=false&fields=stdout,status,time,memory"),
                Headers =
                {
                    { "x-rapidapi-key", "fe05a54966msh74f99cf84607244p1d78dfjsn7624b874c93f" },
                    { "x-rapidapi-host", "judge0-ce.p.rapidapi.com" },
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
        public async Task<IActionResult> Create(int ProblemId, string Code, int LanguageId, string CompetitionNotation, int Year, string ProblemNotation)
        {
            Tuple<string[], string[]> testFiles = GetTestFiles(CompetitionNotation, Year, ProblemNotation);   
            var inputTestFiles = testFiles.Item1;
            var outputTestFiles = testFiles.Item2;

            var problem = await problemsManager.ReadAsync(ProblemId, true);
            Submission submission = new Submission(ProblemId, DateTime.Now);
            await manager.CreateAsync(submission);

            string jsonRequest = await CreatePostQuery(inputTestFiles, outputTestFiles, LanguageId, Code, problem.TimeLimit, problem.MemoryLimit * 666);
            
            var client = httpClientFactory.CreateClient();

            List<PostResponse> tokens = (await PostRequest(jsonRequest, client));
            await Task.Delay(10000);

            BatchSubmissionResponse results = await GetRequest(tokens, client);
            
            submission.TestCaseResults = new List<TestCaseResult>();
            foreach(var result in results.Submissions)
            {
                // Add the result to the results list
                TestCaseResult testCaseResult = new TestCaseResult(submission.Id, result.status.description, (double?)result.memory ?? 0.0, result.stdout ?? "", result.time ?? "0.0");
                submission.TestCaseResults.Add(testCaseResult);
                await testCaseResultManager.CreateAsync(testCaseResult);

            }
            submission.Points = submission.TestCaseResults.Count(r => r.State == "Accepted");
            submission.Time = 1;
            submission.Error = submission.TestCaseResults.FirstOrDefault(r => r.State != "Accepted")?.State;
            submission.Memory = 102444;
            await manager.UpdateAsync(submission, true);
            // Pass the results to the Results view
            return View("Details", submission);
        }

        public async Task<IActionResult> OutputDetails(int id)
        {
            TestCaseResult testCaseResult = await testCaseResultManager.ReadAsync(id);
            return View("OutputDetails", testCaseResult.Output);
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
