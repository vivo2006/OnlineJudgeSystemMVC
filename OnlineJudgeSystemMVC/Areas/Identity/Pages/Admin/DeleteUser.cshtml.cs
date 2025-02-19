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

namespace OnlineJudgeSystemMVC.Areas.Identity.Pages.Admin.DeleteUser
{
    public class DeleteUserModel : PageModel
    {
        private readonly IdentityManager _identityManager;

        public DeleteUserModel(
            IdentityManager identityManager)
        {
            _identityManager = identityManager;
        }
        public ApplicationUser PageUser { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            PageUser = await _identityManager.ReadUserByNameAsync(id);
            return Page();
        }
        public async Task<IActionResult> OnPostAsync(string id)
        {
            await _identityManager.DeleteUserByNameAsync(id);
            return RedirectToPage("AdminIndex");
        }
    }
}
