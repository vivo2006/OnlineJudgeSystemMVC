using BusinessLayer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class ProblemContext : IDb<Problem, int>
    {
        private readonly OnlineJudgeDbContext _context;
        public ProblemContext(OnlineJudgeDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Problem item)
        {
            try
            {
                _context.Problems.Add(item);
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
                Problem problemFromDb = await ReadAsync(key, false, false);

                if (problemFromDb != null)
                {
                    _context.Problems.Remove(problemFromDb);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new ArgumentException("Problem with this id was not found");
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ICollection<Problem>> ReadAllAsync(bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<Problem> query = _context.Problems;
                if (navigationalProperties)
                {
                    query = query.Include(problem => problem.Competition);
                    query = query.Include(problem => problem.Submissions);
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

        public async Task<Problem> ReadAsync(int key, bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<Problem> query = _context.Problems;
                if (navigationalProperties)
                {
                    query = query.Include(problem => problem.Submissions);
                    query = query.Include(problem => problem.Competition);
                }

                if (isReadOnly)
                {
                    query = query.AsNoTrackingWithIdentityResolution();
                }
                return await query.FirstOrDefaultAsync(problem => problem.Id == key);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public Task<ICollection<Problem>> ReadByIdAsync(string key, bool useNavigationalProperties, bool isReadOnly)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateAsync(Problem item, bool navigationalProperties = false)
        {
            try
            {
                Problem problemFromDb = await ReadAsync(item.Id, navigationalProperties, false);
                _context.Entry(problemFromDb).CurrentValues.SetValues(item);

                if (navigationalProperties)
                {
                    problemFromDb.Submissions = item.Submissions;
                    problemFromDb.CompetitionId = item.CompetitionId;

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
