using BusinessLayer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class SubmissionContext : IDb<Submission, int>
    {
        private readonly OnlineJudgeDbContext _context;
        public SubmissionContext(OnlineJudgeDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Submission item)
        {
            try
            {
                item.Created = DateTime.Now;
                _context.Submissions.Add(item);
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
                Submission submissionFromDb = await ReadAsync(key, false, false);

                if (submissionFromDb != null)
                {
                    _context.Submissions.Remove(submissionFromDb);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new ArgumentException("Submission with this id was not found");
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ICollection<Submission>> ReadAllAsync(bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<Submission> query = _context.Submissions;
                if (navigationalProperties)
                {
                    //query = query.Include(submission => submission.User);
                    query = query.Include(submission => submission.Problem);
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

        public async Task<Submission> ReadAsync(int key, bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<Submission> query = _context.Submissions;
                if (navigationalProperties)
                {
                    //query = query.Include(submission => submission.User);
                    query = query.Include(submission => submission.Problem);
                }

                if (isReadOnly)
                {
                    query = query.AsNoTrackingWithIdentityResolution();
                }
                return await query.FirstOrDefaultAsync(submission => submission.Id == key);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task UpdateAsync(Submission item, bool navigationalProperties = false)
        {
            try
            {
                Submission submissionFromDb = await ReadAsync(item.Id, navigationalProperties, false);
                _context.Entry(submissionFromDb).CurrentValues.SetValues(item);
                _context.Entry(submissionFromDb).Property(submission => submission.Created).IsModified = false;

                if (navigationalProperties)
                {
                    submissionFromDb.ProblemId = item.ProblemId;
                    //submissionFromDb.UserId = item.UserId;
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
