using BusinessLayer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Contexts
{
    public interface IDb<T, K>
    {
        Task CreateAsync(T item);
        Task<T> ReadAsync(K key, bool navigationalProperties = false, bool isReadOnly = true);
        Task<ICollection<T>> ReadAllAsync(bool navigationalProperties = false, bool isReadOnly = true);
        Task UpdateAsync(T item, bool navigationalProperties = false);
        Task DeleteAsync(K key);
        public void DeleteDirectory(string prefix, string deleteType, string competitionNotation, string year, string problemNotation)
        {
            try
            {
                string competitionPath = Path.Combine(prefix, deleteType, competitionNotation, year, problemNotation + (deleteType == "Problems" ? ".pdf" : ""));
                if (deleteType == "Problems")
                {
                    if (File.Exists(competitionPath))
                    {
                        File.Delete(competitionPath);
                    }
                    else
                    {
                        throw new ArgumentException("Path not found");
                    }
                }
                else
                {
                    bool a = Directory.Exists(competitionPath);
                    if (Directory.Exists(competitionPath))
                    {
                        Directory.Delete(competitionPath, true);
                    }
                    else
                    {
                        throw new ArgumentException("Directory not found");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting directory: {ex.Message}");
            }
        }
    }
}
