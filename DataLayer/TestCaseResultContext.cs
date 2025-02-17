using BusinessLayer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class TestCaseResultContext : IDb<TestCaseResult, int>
    {
        private readonly OnlineJudgeDbContext _context;
        public TestCaseResultContext(OnlineJudgeDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(TestCaseResult item)
        {
            try
            {
                _context.TestCaseResults.Add(item);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteAsync(int key)
        {
            try
            {
                TestCaseResult testCaseResultFromDb = await ReadAsync(key, false, false);

                if (testCaseResultFromDb != null)
                {
                    _context.TestCaseResults.Remove(testCaseResultFromDb);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new ArgumentException("Test Case Result with this id was not found");
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ICollection<TestCaseResult>> ReadAllAsync(bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<TestCaseResult> query = _context.TestCaseResults;
                if (navigationalProperties)
                {
                    query = query.Include(testCaseResult => testCaseResult.Submission);
                }

                if (isReadOnly)
                {
                    query = query.AsNoTrackingWithIdentityResolution();
                }
                return await query.ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<TestCaseResult> ReadAsync(int key, bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<TestCaseResult> query = _context.TestCaseResults;
                if (navigationalProperties)
                {
                    query = query.Include(testCaseResult => testCaseResult.Submission);
                }

                if (isReadOnly)
                {
                    query = query.AsNoTrackingWithIdentityResolution();
                }
                return await query.FirstOrDefaultAsync(testCaseResult => testCaseResult.Id == key);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task UpdateAsync(TestCaseResult item, bool navigationalProperties = false)
        {
            try
            {
                TestCaseResult competitionFromDb = await ReadAsync(item.Id, navigationalProperties, false);
                _context.Entry(competitionFromDb).CurrentValues.SetValues(item);
                if (navigationalProperties)
                {
                    competitionFromDb.Submission = item.Submission;
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
