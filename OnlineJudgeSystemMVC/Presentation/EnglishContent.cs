using BusinessLayer.Entities;

namespace OnlineJudgeSystemMVC.Presentation;

/// <summary>English display text for the bundled archive; persisted identifiers and files remain unchanged.</summary>
public static class EnglishContent
{
    public sealed record Statement(string Title, string Url);
    private static readonly IReadOnlyDictionary<string, Statement> Statements = new Dictionary<string, Statement>(StringComparer.OrdinalIgnoreCase)
    {
        ["/Problems/\u041d\u0415\u0422\u0418/2010/A1.pdf"] = new("Note2 - Bitwise AND subarrays", "/Problems/english/neti-2010-a1.html"),
        ["/Problems/\u041d\u0415\u0422\u0418/2010/A11.pdf"] = new("Note2 - Bitwise AND subarrays", "/Problems/english/neti-2010-a11.html"),
        ["/Problems/\u041d\u0415\u0422\u0418/2010/E1.pdf"] = new("A+B", "/Problems/english/neti-2010-e1.html"),
        ["/Problems/\u041d\u041e\u04181/2010/E1.pdf"] = new("A+B", "/Problems/english/noi1-2010-e1.html"),
        ["/Problems/\u041d\u0415\u0422\u0418/2010/E4.pdf"] = new("LIKE", "/Problems/english/neti-2010-e4.html"),
        ["/Problems/\u041d\u0415\u0422\u0418/2011/E1.pdf"] = new("Think of a number", "/Problems/english/neti-2011-e1.html"),
        ["/Problems/\u041d\u0415\u0422\u0418/2011/E2.pdf"] = new("Rectangle", "/Problems/english/neti-2011-e2.html"),
        ["/Problems/\u041d\u0415\u0422\u0418/2011/E3.pdf"] = new("Gift", "/Problems/english/neti-2011-e3.html"),
        ["/Problems/\u041d\u0415\u0422\u0418/2015/E1.pdf"] = new("Maya the Bee", "/Problems/english/neti-2015-e1.html"),
    };

    private static string PathFrom(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        var path = url.Replace('\\', '/');
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute) && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps)) path = absolute.AbsolutePath;
        else path = path.Split('?', '#')[0];
        return Uri.UnescapeDataString(path);
    }

    public static string StatementUrl(string? originalUrl) => Statements.TryGetValue(PathFrom(originalUrl), out var statement)
        ? statement.Url : originalUrl ?? "";

    public static string ProblemName(Problem problem)
    {
        var path = PathFrom(problem.URL);
        if (Statements.TryGetValue(path, out var statement)) return statement.Title;
        var translated = Statements.Values.FirstOrDefault(s => string.Equals(s.Url, path, StringComparison.OrdinalIgnoreCase));
        return translated?.Title ?? problem.Name;
    }

    private sealed record CompetitionText(string Name, string Code);
    private static readonly IReadOnlyDictionary<string, CompetitionText> CompetitionNames = new Dictionary<string, CompetitionText>(StringComparer.OrdinalIgnoreCase)
    {
        ["\u041f\u0440\u043e\u043b\u0435\u0442\u0435\u043d \u0442\u0443\u0440\u043d\u0438\u0440 \u043f\u043e \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0442\u0438\u043a\u0430"] = new("Spring Informatics Tournament", "Spring"),
        ["\u041b\u0435\u0442\u0435\u043d \u0442\u0443\u0440\u043d\u0438\u0440 \u043f\u043e \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0442\u0438\u043a\u0430"] = new("Summer Informatics Tournament", "Summer"),
        ["\u041d\u0430\u0446\u0438\u043e\u043d\u0430\u043b\u043d\u0430 \u043e\u043b\u0438\u043c\u043f\u0438\u0430\u0434\u0430 \u043f\u043e \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0442\u0438\u043a\u0430 - \u043e\u0431\u043b\u0430\u0441\u0442\u0435\u043d \u043a\u0440\u044a\u0433"] = new("National Olympiad in Informatics - Regional Round", "NOI Regional"),
        ["\u041d\u0430\u0446\u0438\u043e\u043d\u0430\u043b\u043d\u0430 \u043e\u043b\u0438\u043c\u043f\u0438\u0430\u0434\u0430 \u043f\u043e \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0442\u0438\u043a\u0430 - \u043d\u0430\u0446\u0438\u043e\u043d\u0430\u043b\u0435\u043d \u043a\u0440\u044a\u0433"] = new("National Olympiad in Informatics - National Round", "NOI National"),
    };
    private static readonly IReadOnlyDictionary<string, CompetitionText> CompetitionCodes = new Dictionary<string, CompetitionText>(StringComparer.OrdinalIgnoreCase)
    {
        ["\u041d\u0415\u0422\u0418"] = new("National Autumn Informatics Tournament", "NETI"),
        ["NETI"] = new("National Autumn Informatics Tournament", "NETI"),
        ["\u041d\u041e\u04181"] = new("National Olympiad in Informatics - Round 1", "NOI Round 1"),
        ["NOI1"] = new("National Olympiad in Informatics - Round 1", "NOI Round 1"),
        ["\u041d\u041e\u04182"] = new("National Olympiad in Informatics - Regional Round", "NOI Regional"),
        ["NOI2"] = new("National Olympiad in Informatics - Regional Round", "NOI Regional"),
        ["\u041d\u041e\u04183"] = new("National Olympiad in Informatics - National Round", "NOI National"),
        ["NOI3"] = new("National Olympiad in Informatics - National Round", "NOI National"),
        ["\u041d\u041f\u0422\u0418"] = new("Spring Informatics Tournament", "NPTI"),
        ["\u041f\u0422\u0418"] = new("Spring Informatics Tournament", "PTI"),
        ["NPTI"] = new("Spring Informatics Tournament", "NPTI"),
        ["\u041d\u041b\u0422\u0418"] = new("Summer Informatics Tournament", "NLTI"),
        ["\u041b\u0422\u0418"] = new("Summer Informatics Tournament", "LTI"),
        ["NLTI"] = new("Summer Informatics Tournament", "NLTI"),
    };

    private static string NormalizeName(string value) => string.Join(" ", value.Replace('–', '-').Replace('—', '-').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static CompetitionText? FindCompetition(string? notation, string? name = null)
    {
        if (name != null && CompetitionNames.TryGetValue(NormalizeName(name), out var named)) return named;
        return notation != null && CompetitionCodes.TryGetValue(notation.Trim(), out var coded) ? coded : null;
    }

    public static string CompetitionName(Competition competition) => CompetitionLabel(competition.Notation, competition.Name);
    public static string CompetitionLabel(string? notation, string? fallback = null) => FindCompetition(notation, fallback)?.Name ?? fallback ?? notation ?? "Competition";
    public static string CompetitionCode(Competition competition) => FindCompetition(competition.Notation, competition.Name)?.Code ?? competition.Notation;
    public static string CompetitionCode(string? notation) => FindCompetition(notation)?.Code ?? notation ?? "";
}
