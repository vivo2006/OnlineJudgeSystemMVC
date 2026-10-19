namespace DataLayer;

public static class Connection
{
    // Used by design-time tools only. Runtime hosts use their own configuration.
    public static string conString => System.Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? @"Server=(localdb)\MSSQLLocalDB;Database=CodeXpert;Trusted_Connection=True;TrustServerCertificate=True;";
}
