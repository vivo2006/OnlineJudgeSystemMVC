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
        public async Task<IActionResult> UploadFolder(IFormFile[] files, int id)
        {
            if (files == null || files.Length == 0)
            {
                return BadRequest("No files uploaded.");
            }
            
            Problem problem = await manager.ReadAsync(id, true);
            int groupId = problem.CompetitionId;

            GroupCompetition groupCompetition = await groupCompetitionsManager.ReadAsync(groupId, true);
            YearCompetition yearCompetition = await yearCompetitionsManager.ReadAsync(groupCompetition.YearCompetitionId, true);
            Competition competition = await competitionsManager.ReadAsync(yearCompetition.CompetitionId, true);

            var uploadFolder = Path.Combine(_environment.ContentRootPath, "TestCases", competition.CompetitionAnnotation, yearCompetition.Year.ToString(), problem.ProblemNotation);
            var inputFolderPath = Path.Combine(uploadFolder, "Input","tests");
            var outputFolderPath = Path.Combine(uploadFolder, "Output", "tests");

            if (!Directory.Exists(inputFolderPath))
            {
                Directory.CreateDirectory(inputFolderPath);
            }
            bool flag = !Directory.Exists(outputFolderPath);
            if (flag)
            {
                Directory.CreateDirectory(outputFolderPath);
            }


            foreach (var file in files)
            {
                //string fileName = file.FileName.Substring(file.FileName.IndexOf('.') + 1);
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                if (fileExtension == ".in" && file.Length > 0)
                {
                    var inputFilePath = Path.Combine(Path.Combine(uploadFolder, "Input"), file.FileName);

                    using (var stream = new FileStream(Path.Combine(uploadFolder, "Input"), FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
                else if (fileExtension == ".out" && file.Length > 0)
                {
                    var outputFilePath = Path.Combine(Path.Combine(uploadFolder, "Output"), file.FileName);

                    using (var stream = new FileStream(Path.Combine(uploadFolder, "Output"), FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
            }

            return Ok($"{files.Length} files uploaded successfully.");
        }
    }
}
