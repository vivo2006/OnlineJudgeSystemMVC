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
                if (deleteType != "Problems" && deleteType != "TestCases") throw new ArgumentException("Invalid storage type.");
                foreach (var segment in new[] { competitionNotation, year, problemNotation })
                    if (!string.IsNullOrEmpty(segment) && (segment is "." or ".." || segment.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_')))
                        throw new ArgumentException("Invalid storage identifier.");
                if (string.IsNullOrEmpty(prefix)) throw new ArgumentException("An absolute storage root is required.");
                var storageRoot = Path.GetFullPath(Path.Combine(prefix, deleteType));
                string competitionPath = Path.GetFullPath(Path.Combine(storageRoot, competitionNotation, year,
                    problemNotation + (deleteType == "Problems" && !string.IsNullOrEmpty(problemNotation) ? ".pdf" : "")));
                if (!competitionPath.StartsWith(storageRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Storage path must remain below its root.");
                if (deleteType == "Problems" && !string.IsNullOrEmpty(problemNotation))
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
