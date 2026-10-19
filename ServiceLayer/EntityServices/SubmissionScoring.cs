using BusinessLayer.Entities;

namespace ServiceLayer.EntityServices;

public static class SubmissionScoring
{
    public static int[] Calculate(IEnumerable<Submission> submissions)
    {
        var points = new int[5];
        foreach (var problem in submissions.Where(s => s.Status == "Completed").GroupBy(s => s.ProblemId))
        {
            var notation = problem.First().Problem.ProblemNotation;
            int group = string.IsNullOrEmpty(notation) ? -1 : char.ToUpperInvariant(notation[0]) - 'A';
            if (group >= 0 && group < points.Length)
                points[group] += problem.Max(s => s.Points) + problem.Sum(s => s.ChallengeBonus);
        }
        return points;
    }

    public static bool EarnsChallengeBonus(Submission current, ApplicationUser user, IEnumerable<Submission> previous)
    {
        if (current.Points != 100 || user.LastChallengeTaskId != current.ProblemId ||
            user.LastChallengeDate?.Date != current.Created.Date) return false;
        var history = previous.Where(s => s.Id != current.Id && s.ProblemId == current.ProblemId &&
            s.Status == "Completed" && s.Points == 100).ToArray();
        return history.Length > 0 && !history.Any(s => s.Created.Date == current.Created.Date && s.ChallengeBonus > 0) &&
            (current.Time < history.Min(s => s.Time) || current.Memory < history.Min(s => s.Memory));
    }
}
