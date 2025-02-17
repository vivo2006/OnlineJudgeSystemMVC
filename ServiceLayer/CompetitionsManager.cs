using BusinessLayer;
using DataLayer;

namespace ServiceLayer
{
    public class CompetitionsManager
    {
        private readonly IDb<Competition, int> context;

        public CompetitionsManager(IDb<Competition, int> competitionContext)
        {
            context = competitionContext;
        }

        public async Task CreateAsync(Competition competition)
        {
            await context.CreateAsync(competition);
        }
        public async Task<Competition> ReadAsync(int key, bool useNavigationalProperties = false, bool isReadOnly = true)
        {
            return await context.ReadAsync(key, useNavigationalProperties, isReadOnly);
        }

        public async Task<ICollection<Competition>> ReadAllAsync(bool useNavigationalProperties = false, bool isReadOnly = true)
        {
            return await context.ReadAllAsync(useNavigationalProperties, isReadOnly);
        }

        public async Task UpdateAsync(Competition competition, bool useNavigationalProperties = false)
        {
            await context.UpdateAsync(competition, useNavigationalProperties);
        }

        public async Task DeleteAsync(int key)
        {
            await context.DeleteAsync(key);
        }

        public void DeleteDirectory(string deleteType, string competitionNotation, string year = "", string problemNotation = "")
        {
            context.DeleteDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), deleteType, competitionNotation, year, problemNotation);
        }

    }
}
