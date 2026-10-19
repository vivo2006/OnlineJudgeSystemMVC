using BusinessLayer.API;
using DataLayer.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineJudgeSystemMVC.Services;
using ServiceLayer.Storage;

namespace OnlineJudgeSystemMVC.Controllers;

[Authorize(Roles = "Admin")]
public class TestCasesController : Controller
{
    private readonly OnlineJudgeDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly JudgingOptions _options;
    public TestCasesController(OnlineJudgeDbContext db, IWebHostEnvironment environment, IOptions<JudgingOptions> options)
        => (_db, _environment, _options) = (db, environment, options.Value);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadFolder(IFormFile[] files, int id)
    {
        var problem = await _db.Problems.Include(p => p.Competition).ThenInclude(g => g.YearCompetition)
            .ThenInclude(y => y.Competition).SingleOrDefaultAsync(p => p.Id == id);
        if (problem == null) return NotFound();
        try
        {
            var year = problem.Competition.YearCompetition;
            var directory = ProblemStorage.DirectoryFor(Path.Combine(_environment.ContentRootPath, "TestCases"),
                year.Competition.Notation, year.Year, problem.ProblemNotation);
            TestCaseUploads.Validate(files ?? Array.Empty<IFormFile>(), _options.MaxTestCases);
            // Validate all incoming files before replacing the existing test set.
            await TestCaseUploads.SaveAsync(directory, files!, _options.MaxTestCases);
            return Ok("Test cases uploaded.");
        }
        catch (ArgumentException e) { return BadRequest(e.Message); }
    }
}
