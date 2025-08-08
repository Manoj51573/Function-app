using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoT.Infrastructure;

public interface ISpViewRepository<T> where T : class
{
    Task<List<T>> ToListAsync(string sqlCommand, params object[] parameters);
    Task<T> SingleOrDefaultAsync(string sqlCommand, params object[] parameters);
}