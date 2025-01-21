using BusinessLayer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class YearCompetitionContext : IDb<YearCompetition, int>
    {
        private readonly OnlineJudgeDbContext _context;
        public YearCompetitionContext(OnlineJudgeDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(YearCompetition item)
        {
            try
            {
                item.Created = DateTime.Now;
                item.LastModified = DateTime.Now;
                _context.YearCompetitions.Add(item);
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
                YearCompetition yearCompetitionFromDb = await ReadAsync(key, false, false);

                if (yearCompetitionFromDb != null)
                {
                    _context.YearCompetitions.Remove(yearCompetitionFromDb);
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

        public async Task<ICollection<YearCompetition>> ReadAllAsync(bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<YearCompetition> query = _context.YearCompetitions;
                if (navigationalProperties)
                {
                    query = query.Include(yearCompetitionFromDb => yearCompetitionFromDb.GroupCompetitions);
                    query = query.Include(yearCompetitionFromDb => yearCompetitionFromDb.Competition);
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

        public async Task<YearCompetition> ReadAsync(int key, bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<YearCompetition> query = _context.YearCompetitions;
                if (navigationalProperties)
                {
                    query = query.Include(yearCompetitionFromDb => yearCompetitionFromDb.GroupCompetitions);
                    query = query.Include(yearCompetitionFromDb => yearCompetitionFromDb.Competition);
                }

                if (isReadOnly)
                {
                    query = query.AsNoTrackingWithIdentityResolution();
                }
                return await query.FirstOrDefaultAsync(yearCompetitionFromDb => yearCompetitionFromDb.Id == key);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task UpdateAsync(YearCompetition item, bool navigationalProperties = false)
        {
            try
            {
                YearCompetition yearCompetitionFromDb = await ReadAsync(item.Id, navigationalProperties, false);
                _context.Entry(yearCompetitionFromDb).CurrentValues.SetValues(item);
                yearCompetitionFromDb.LastModified = DateTime.Now;
                _context.Entry(yearCompetitionFromDb).Property(yearCompetition => yearCompetition.Created).IsModified = false;
                if (navigationalProperties)
                {
                    yearCompetitionFromDb.GroupCompetitions = item.GroupCompetitions;
                    yearCompetitionFromDb.CompetitionId = item.CompetitionId;
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
