using BusinessLayer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    internal class CompetitionContext : IDb<Competition, int>
    {
        private readonly OnlineJudgeDbContext _context;
        public CompetitionContext(OnlineJudgeDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Competition item)
        {
            try
            {
                _context.Competitions.Add(item);
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
                Competition competitionFromDb = await ReadAsync(key, false, false);

                if (competitionFromDb != null)
                {
                    _context.Competitions.Remove(competitionFromDb);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new ArgumentException("Competition with this id was not found");
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ICollection<Competition>> ReadAllAsync(bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<Competition> query = _context.Competitions;
                if (navigationalProperties)
                {
                    query = query.Include(competition => competition.GroupCompetitions);
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

        public async Task<Competition> ReadAsync(int key, bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<Competition> query = _context.Competitions;
                if (navigationalProperties)
                {
                    query = query.Include(competition => competition.GroupCompetitions);
                }

                if (isReadOnly)
                {
                    query = query.AsNoTrackingWithIdentityResolution();
                }
                return await query.FirstOrDefaultAsync(competition => competition.Id == key);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task UpdateAsync(Competition item, bool navigationalProperties = false)
        {
            try
            {
                Competition competitionFromDb = await ReadAsync(item.Id, navigationalProperties, false);
                _context.Entry(competitionFromDb).CurrentValues.SetValues(item);
                if (navigationalProperties)
                {
                    competitionFromDb.GroupCompetitions = item.GroupCompetitions;
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
