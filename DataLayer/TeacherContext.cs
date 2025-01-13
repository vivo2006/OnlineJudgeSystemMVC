using BusinessLayer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    internal class TeacherContext : IDb<Teacher, string>
    {
        private readonly OnlineJudgeDbContext _context;
        public TeacherContext(OnlineJudgeDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Teacher item)
        {
            try
            {
                _context.Teachers.Add(item);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteAsync(string key)
        {
            try
            {
                Teacher teacherFromDb = await ReadAsync(key, false, false);

                if (teacherFromDb != null)
                {
                    _context.Teachers.Remove(teacherFromDb);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new ArgumentException("Teacher with this id was not found");
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ICollection<Teacher>> ReadAllAsync(bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<Teacher> query = _context.Teachers;
                if (navigationalProperties)
                {
                    query = query.Include(teacher => teacher.Submissions);
                    query = query.Include(teacher => teacher.Students);
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

        public async Task<Teacher> ReadAsync(string key, bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<Teacher> query = _context.Teachers;
                if (navigationalProperties)
                {
                    query = query.Include(teacher => teacher.Students);
                    query = query.Include(teacher => teacher.Submissions);
                }

                if (isReadOnly)
                {
                    query = query.AsNoTrackingWithIdentityResolution();
                }
                return await query.FirstOrDefaultAsync(teacher => teacher.Id == key);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task UpdateAsync(Teacher item, bool NavigationalProperties = false)
        {
            try
            {
                Teacher teacherFromDb = await ReadAsync(item.Id, NavigationalProperties, false);
                _context.Entry(teacherFromDb).CurrentValues.SetValues(item);
                if (NavigationalProperties)
                {
                    teacherFromDb.Students = item.Students;
                    teacherFromDb.Submissions = item.Submissions;
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
