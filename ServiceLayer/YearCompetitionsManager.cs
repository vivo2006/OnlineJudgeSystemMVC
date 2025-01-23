using BusinessLayer;
using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer
{
    public class YearCompetitionsManager
    {
        private readonly IDb<YearCompetition, int> context;

        public YearCompetitionsManager(IDb<YearCompetition, int> competitionContext)
        {
            context = competitionContext;
        }

        public async Task CreateAsync(YearCompetition competition)
        {
            await context.CreateAsync(competition);
        }
        public async Task UpdateAsync(YearCompetition competition)
        {
            await context.UpdateAsync(competition);
        }
        public async Task<YearCompetition> ReadAsync(int key, bool useNavigationalProperties = false, bool isReadOnly = true)
        {
            return await context.ReadAsync(key, useNavigationalProperties, isReadOnly);
        }
        public async Task<ICollection<YearCompetition>> ReadAllAsync(bool useNavigationalProperties = false, bool isReadOnly = true)
        {
            return await context.ReadAllAsync(useNavigationalProperties, isReadOnly);
        }
        public async Task DeleteAsync(int key)
        {
            await context.DeleteAsync(key);
        }
    }
}
