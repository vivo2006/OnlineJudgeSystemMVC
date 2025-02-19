using BusinessLayer.Entities;
using DataLayer.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.EntityServices
{
    public class TestCaseResultsManager
    {
        private readonly IDb<TestCaseResult, int> context;

        public TestCaseResultsManager(IDb<TestCaseResult, int> testCaseResultContext)
        {
            context = testCaseResultContext;
        }

        public async Task CreateAsync(TestCaseResult testCaseResult)
        {
            await context.CreateAsync(testCaseResult);
        }
        public async Task<TestCaseResult> ReadAsync(int key, bool useNavigationalProperties = false, bool isReadOnly = true)
        {
            return await context.ReadAsync(key, useNavigationalProperties, isReadOnly);
        }

        public async Task<ICollection<TestCaseResult>> ReadAllAsync(bool useNavigationalProperties = false, bool isReadOnly = true)
        {
            return await context.ReadAllAsync(useNavigationalProperties, isReadOnly);
        }

        public async Task UpdateAsync(TestCaseResult testCaseResult, bool useNavigationalProperties = false)
        {
            await context.UpdateAsync(testCaseResult, useNavigationalProperties);
        }

        public async Task DeleteAsync(int key)
        {
            await context.DeleteAsync(key);
        }
    }
}
