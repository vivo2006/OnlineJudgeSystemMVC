// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BusinessLayer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServiceLayer;
using X.PagedList.Extensions;

namespace OnlineJudgeSystemMVC.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly SubmissionsManager _submissionsManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, 
            SubmissionsManager submissionsManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _submissionsManager = submissionsManager;
        }
        public X.PagedList.IPagedList<Submission> Submissions { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            int pageSize = 10; // Number of rows per page
            int pageNumber = (id ?? 1); // Default to first page if none specified

            Submissions = (await _submissionsManager.ReadByIdAsync(user.Id, true)).ToPagedList(pageNumber, pageSize);
            return Page();
        }
    }
}
