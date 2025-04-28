using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using BusinessLayer.Entities;
using ServiceLayer.EntityServices;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class TestCasesController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ProblemsManager _problemsManager;
        private readonly GroupCompetitionsManager _groupManager;
        private readonly YearCompetitionsManager _yearManager;
        private readonly CompetitionsManager _competitionManager;

        public TestCasesController(
            IWebHostEnvironment env,
            ProblemsManager problemsManager,
            GroupCompetitionsManager groupManager,
            YearCompetitionsManager yearManager,
            CompetitionsManager competitionManager)
        {
            _env = env;
            _problemsManager = problemsManager;
            _groupManager = groupManager;
            _yearManager = yearManager;
            _competitionManager = competitionManager;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UploadFolder(IFormFile[] files, int id)
        {
            if (files == null || files.Length == 0)
                return BadRequest("No files uploaded.");

            var problem = await _problemsManager.ReadAsync(id, true);
            var group = await _groupManager.ReadAsync(problem.CompetitionId, true);
            var year = await _yearManager.ReadAsync(group.YearCompetitionId, true);
            var competition = await _competitionManager.ReadAsync(year.CompetitionId, true);

            string uploadPath = Path.Combine(_env.ContentRootPath, "TestCases", competition.Notation, year.Year.ToString(), problem.ProblemNotation);
            string inputPath = Path.Combine(uploadPath, "Input", "tests");
            string outputPath = Path.Combine(uploadPath, "Output", "tests");

            Directory.CreateDirectory(inputPath);
            Directory.CreateDirectory(outputPath);

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.FileName).ToLower();
                if (file.Length == 0) continue;

                string targetPath = ext switch
                {
                    ".in" => Path.Combine(uploadPath, "Input", file.FileName),
                    ".out" => Path.Combine(uploadPath, "Output", file.FileName),
                    ".sol" => Path.Combine(uploadPath, "Output", file.FileName),
                    _ => null
                };

                if (targetPath != null)
                {
                    using var stream = new FileStream(targetPath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
            }

            return Ok($"{files.Length} files uploaded successfully.");
        }
    }
}