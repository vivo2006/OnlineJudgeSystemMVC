using BusinessLayer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    internal class StudentContext : IDb<Student, string>
    {
        private readonly OnlineJudgeDbContext _context;
        public StudentContext(OnlineJudgeDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Student item)
        {
            try
            {
                _context.Students.Add(item);
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
                Student studentFromDb = await ReadAsync(key, false, false);

                if (studentFromDb != null)
                {
                    _context.Students.Remove(studentFromDb);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new ArgumentException("Student with this id was not found");
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ICollection<Student>> ReadAllAsync(bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<Student> query = _context.Students;
                if (navigationalProperties)
                {
                    query = query.Include(student => student.Teachers);
                    query = query.Include(student => student.SolvedProblems);
                    query = query.Include(student => student.Submissions);
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

        public async Task<Student> ReadAsync(string key, bool navigationalProperties = false, bool isReadOnly = true)
        {
            try
            {
                IQueryable<Student> query = _context.Students;
                if (navigationalProperties)
                {
                    query = query.Include(student => student.SolvedProblems);
                    query = query.Include(student => student.Teachers);
                    query = query.Include(student => student.Submissions);
                }

                if (isReadOnly)
                {
                    query = query.AsNoTrackingWithIdentityResolution();
                }
                return await query.FirstOrDefaultAsync(student => student.Id == key);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task UpdateAsync(Student item, bool NavigationalProperties = false)
        {
            try
            {
                Student studentFromDb = await ReadAsync(item.Id, NavigationalProperties, false);
                _context.Entry(studentFromDb).CurrentValues.SetValues(item);
                if (NavigationalProperties)
                {
                    studentFromDb.Submissions = item.Submissions;
                    studentFromDb.Teachers = item.Teachers;
                    studentFromDb.SolvedProblems = item.SolvedProblems;
                    

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
