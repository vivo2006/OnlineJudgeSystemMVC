using BusinessLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.EntityServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class LeaderboardModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityManager _identityManager;

    public LeaderboardModel(UserManager<ApplicationUser> userManager, IdentityManager identityManager)
    {
        _userManager = userManager;
        _identityManager = identityManager;
    }

    public List<UserRank> Leaderboard { get; set; }

    public class UserRank
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int[] Points { get; set; }
    }

    public async Task OnGetAsync()
    {
        // Get all users and sort them by points in descending order
        var users = (await _identityManager.ReadAllUsersAsync())
            .Select(user => new UserRank
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Points = user.Points
            })
            .ToList();

        Leaderboard = users;
    }
}
