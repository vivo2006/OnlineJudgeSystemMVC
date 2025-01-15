using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public interface IDb<T, K>
    {
        Task CreateAsync(T item);
        Task<T> ReadAsync(K key, bool navigationalProperties = false, bool isReadOnly = true);
        Task<ICollection<T>> ReadAllAsync(bool navigationalProperties = false, bool isReadOnly = true);
        Task UpdateAsync(T item, bool navigationalProperties = false);
        Task DeleteAsync(K key);
    }
}
