using BusinessLayer;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer;
using System.Text.RegularExpressions;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class TestCasesController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ProblemsManager manager;
        private readonly GroupCompetitionsManager groupCompetitionsManager;
        private readonly YearCompetitionsManager yearCompetitionsManager;
        private readonly CompetitionsManager competitionsManager;

        public TestCasesController(IWebHostEnvironment environment, ProblemsManager manager, GroupCompetitionsManager groupCompetitionsManager, YearCompetitionsManager yearCompetitionsManager, CompetitionsManager competitionsManager)
        {
            _environment = environment;
            this.manager = manager;
            this.groupCompetitionsManager = groupCompetitionsManager;
            this.yearCompetitionsManager = yearCompetitionsManager;
            this.competitionsManager = competitionsManager;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFolder(IFormFile[] files, int groupId, int problemId)
        {
            if (files == null || files.Length == 0)
            {
                return BadRequest("No files uploaded.");
            }

            GroupCompetition groupCompetition = await groupCompetitionsManager.ReadAsync(groupId, true);
            YearCompetition yearCompetition = await yearCompetitionsManager.ReadAsync(groupCompetition.YearCompetitionId, true);
            Competition competition = await competitionsManager.ReadAsync(yearCompetition.CompetitionId, true);
            Problem problem = await manager.ReadAsync(problemId, true);

            var uploadFolder = Path.Combine(_environment.ContentRootPath, "TestCases", competition.CompetitionAnnotation, yearCompetition.Year.ToString(), problem.ProblemNotation);

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var filePath = Path.Combine(uploadFolder, file.FileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        return BadRequest($"File {file.FileName} already exists.");
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
            }

            return Ok($"{files.Length} files uploaded successfully.");
        }
    }
}
