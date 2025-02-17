using BusinessLayer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class GroupCompetitionContext : IDb<GroupCompetition, int>
    {
        private readonly OnlineJudgeDbContext _context;
        public GroupCompetitionContext(OnlineJudgeDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(GroupCompetition item)
        {
            try
            {
                _context.GroupCompetitions.Add(item);
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
                GroupCompetition groupCompetitionFromDb = await ReadAsync(key, false, false);

                if (groupCompetitionFromDb != null)
                {
                    _context.GroupCompetitions.Remove(groupCompetitionFromDb);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new ArgumentException("Group Competition with this id was not found");
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ICollection<GroupCompetition>> ReadAllAsync(bool NavigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<GroupCompetition> query = _context.GroupCompetitions;
                if (NavigationalProperties)
                {
                    query = query.Include(groupCompetition => groupCompetition.YearCompetition);
                    query = query.Include(groupCompetition => groupCompetition.Problems);
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

        public async Task<GroupCompetition> ReadAsync(int key, bool NavigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<GroupCompetition> query = _context.GroupCompetitions;
                if (NavigationalProperties)
                {
                    query = query.Include(groupCompetition => groupCompetition.Problems);
                    query = query.Include(groupCompetition => groupCompetition.YearCompetition);
                }

                if (isReadOnly)
                {
                    query = query.AsNoTrackingWithIdentityResolution();
                }
                return await query.FirstOrDefaultAsync(groupCompetition => groupCompetition.Id == key);
            }
            catch (Exception)
            {

                throw;
            }
        }


        public async Task UpdateAsync(GroupCompetition item, bool NavigationalProperties = false)
        {
            try
            {
                GroupCompetition groupCompetitionFromDb = await ReadAsync(item.Id, NavigationalProperties, false);
                _context.Entry(groupCompetitionFromDb).CurrentValues.SetValues(item);

                if (NavigationalProperties)
                {
                    groupCompetitionFromDb.Problems = item.Problems;
                    groupCompetitionFromDb.YearCompetitionId = item.YearCompetitionId;

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
