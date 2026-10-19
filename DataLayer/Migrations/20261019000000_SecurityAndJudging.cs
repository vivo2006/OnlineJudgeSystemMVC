using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations;

public partial class SecurityAndJudging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Earlier model-only constraints were never captured in the initial migration.
        // Stop on conflicting legacy data rather than truncate or delete anything.
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM Competitions WHERE LEN(Notation) > 450)
                THROW 51000, 'Competition identifiers exceed 450 characters. Correct them before upgrading.', 1;
            IF EXISTS (SELECT 1 FROM Problems WHERE LEN(ProblemNotation) > 450)
                THROW 51000, 'Problem identifiers exceed 450 characters. Correct them before upgrading.', 1;
            IF EXISTS (SELECT Notation FROM Competitions GROUP BY Notation HAVING COUNT(*) > 1)
                THROW 51000, 'Duplicate competition identifiers must be corrected before upgrading.', 1;
            IF EXISTS (SELECT g.YearCompetitionId, p.ProblemNotation FROM Problems p
                JOIN GroupCompetitions g ON p.CompetitionId = g.Id
                GROUP BY g.YearCompetitionId, p.ProblemNotation HAVING COUNT(*) > 1)
                THROW 51000, 'Duplicate problem identifiers within a competition year must be corrected before upgrading.', 1;
            """);
        migrationBuilder.AlterColumn<string>("Notation", "Competitions", type: "nvarchar(450)", nullable: false,
            oldClrType: typeof(string), oldType: "nvarchar(max)");
        migrationBuilder.AlterColumn<string>("ProblemNotation", "Problems", type: "nvarchar(450)", nullable: false,
            oldClrType: typeof(string), oldType: "nvarchar(max)");
        migrationBuilder.AddColumn<int>("YearCompetitionId", "Problems", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.Sql("UPDATE p SET YearCompetitionId = g.YearCompetitionId FROM Problems p JOIN GroupCompetitions g ON p.CompetitionId = g.Id");
        migrationBuilder.CreateIndex("IX_Competitions_Notation", "Competitions", "Notation", unique: true);
        migrationBuilder.CreateIndex("IX_Problems_YearCompetitionId_ProblemNotation", "Problems", new[] { "YearCompetitionId", "ProblemNotation" }, unique: true);
        migrationBuilder.AddForeignKey("FK_Problems_YearCompetitions_YearCompetitionId", "Problems", "YearCompetitionId",
            "YearCompetitions", principalColumn: "Id", onDelete: ReferentialAction.NoAction);
        migrationBuilder.Sql("""
            UPDATE p SET URL = '/Problems/' + c.Notation + '/' + CONVERT(varchar(10), y.Year) + '/' + p.ProblemNotation + '.pdf'
            FROM Problems p JOIN GroupCompetitions g ON p.CompetitionId = g.Id
            JOIN YearCompetitions y ON g.YearCompetitionId = y.Id JOIN Competitions c ON y.CompetitionId = c.Id
            WHERE p.URL LIKE 'https://localhost%' OR p.URL LIKE 'http://localhost%';
            """);
        migrationBuilder.AddColumn<string>("Status", "Submissions", type: "nvarchar(30)", maxLength: 30,
            nullable: false, defaultValue: "Completed");
        migrationBuilder.AddColumn<int>("LanguageId", "Submissions", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>("ChallengeBonus", "Submissions", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.CreateIndex("IX_Submissions_UserId_Created", "Submissions", new[] { "UserId", "Created" });
        // Legacy rows without test results cannot be assumed to have completed judging.
        migrationBuilder.Sql("UPDATE Submissions SET Status = 'Incomplete' WHERE NOT EXISTS (SELECT 1 FROM TestCaseResults WHERE TestCaseResults.SubmissionId = Submissions.Id)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Problems_YearCompetitions_YearCompetitionId", "Problems");
        migrationBuilder.DropIndex("IX_Problems_YearCompetitionId_ProblemNotation", "Problems");
        migrationBuilder.DropIndex("IX_Competitions_Notation", "Competitions");
        migrationBuilder.DropColumn("YearCompetitionId", "Problems");
        migrationBuilder.AlterColumn<string>("Notation", "Competitions", type: "nvarchar(max)", nullable: false,
            oldClrType: typeof(string), oldType: "nvarchar(450)");
        migrationBuilder.AlterColumn<string>("ProblemNotation", "Problems", type: "nvarchar(max)", nullable: false,
            oldClrType: typeof(string), oldType: "nvarchar(450)");
        migrationBuilder.DropIndex("IX_Submissions_UserId_Created", "Submissions");
        migrationBuilder.DropColumn("Status", "Submissions");
        migrationBuilder.DropColumn("LanguageId", "Submissions");
        migrationBuilder.DropColumn("ChallengeBonus", "Submissions");
    }
}
