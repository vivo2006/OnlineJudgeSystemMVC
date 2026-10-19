using Hangfire.Dashboard;

namespace OnlineJudgeSystemMVC.Services;

public sealed class AdminDashboardAuthorization : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) =>
        context.GetHttpContext().User.Identity?.IsAuthenticated == true &&
        context.GetHttpContext().User.IsInRole("Admin");
}
