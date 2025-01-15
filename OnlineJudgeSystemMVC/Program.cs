using BusinessLayer;
using DataLayer;
using Microsoft.EntityFrameworkCore;
using ServiceLayer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<OnlineJudgeDbContext>(options => options.UseSqlServer(Connection.conString));
builder.Services.AddScoped<ProblemsManager, ProblemsManager>();
builder.Services.AddScoped<IDb<Problem, int>, ProblemContext>();
builder.Services.AddScoped<ProblemContext, ProblemContext>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
