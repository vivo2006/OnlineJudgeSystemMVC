using BusinessLayer.Entities;
using DataLayer.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.EntityServices
{
    public class GroupCompetitionsManager
    {
        private readonly IDb<GroupCompetition, int> context;

        public GroupCompetitionsManager(IDb<GroupCompetition, int> competitionContext)
        {
            context = competitionContext;
        }

        public async Task CreateAsync(GroupCompetition competition)
        {
            await context.CreateAsync(competition);
        }
        public async Task<GroupCompetition> ReadAsync(int key, bool useNavigationalProperties = false, bool isReadOnly = true)
        {
            return await context.ReadAsync(key, useNavigationalProperties, isReadOnly);
        }

        public async Task<ICollection<GroupCompetition>> ReadAllAsync(bool useNavigationalProperties = false, bool isReadOnly = true)
        {
            return await context.ReadAllAsync(useNavigationalProperties, isReadOnly);
        }

        public async Task UpdateAsync(GroupCompetition competition, bool useNavigationalProperties = false)
        {
            await context.UpdateAsync(competition, useNavigationalProperties);
        }

        public async Task DeleteAsync(int key)
        {
            await context.DeleteAsync(key);
        }

    }
}
