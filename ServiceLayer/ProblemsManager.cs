using BusinessLayer;
using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer
{
    public class ProblemsManager
    {
        private readonly IDb<Problem, int> context;

        public ProblemsManager(IDb<Problem, int> problemContext)
        {
            context = problemContext;
        }

        public async Task CreateAsync(Problem problem)
        {
            await context.CreateAsync(problem);
        }
        public async Task<Problem> ReadAsync(int key, bool useNavigationalProperties = false, bool isReadOnly = true)
        {
            return await context.ReadAsync(key, useNavigationalProperties, isReadOnly);
        }

        public async Task<ICollection<Problem>> ReadAllAsync(bool useNavigationalProperties = false, bool isReadOnly = true)
        {
            return await context.ReadAllAsync(useNavigationalProperties, isReadOnly);
        }

        public async Task UpdateAsync(Problem problem, bool useNavigationalProperties = false)
        {
            await context.UpdateAsync(problem, useNavigationalProperties);
        }

        public async Task DeleteAsync(int key)
        {
            await context.DeleteAsync(key);
        }

    }
}
