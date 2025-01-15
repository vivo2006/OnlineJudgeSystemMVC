using BusinessLayer;
using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer
{
    public class SubmissionsManager
    {
        private readonly IDb<Submission, int> context;

        public SubmissionsManager(IDb<Submission, int> submissionContext)
        {
            context = submissionContext;
        }

        public async Task CreateAsync(Submission submission)
        {
            await context.CreateAsync(submission);
        }
        public async Task<Submission> ReadAsync(int key, bool useNavigationalProperties = false, bool isReadOnly = true)
        {
            return await context.ReadAsync(key, useNavigationalProperties, isReadOnly);
        }

        public async Task<ICollection<Submission>> ReadAllAsync(bool useNavigationalProperties = false, bool isReadOnly = true)
        {
            return await context.ReadAllAsync(useNavigationalProperties, isReadOnly);
        }

        public async Task UpdateAsync(Submission submission, bool useNavigationalProperties = false)
        {
            await context.UpdateAsync(submission, useNavigationalProperties);
        }

        public async Task DeleteAsync(int key)
        {
            await context.DeleteAsync(key);
        }
    }
}
