namespace BusinessLayer.API;

public class JudgingOptions
{
    public int ResultDelaySeconds { get; set; } = 10;
    public int MaxTestCases { get; set; } = 20;
    public int MaxSubmissionsPerDay { get; set; } = 10;
    public int MaxGlobalSubmissionsPerDay { get; set; } = 10;
    public int SubmissionCooldownSeconds { get; set; } = 60;
}
