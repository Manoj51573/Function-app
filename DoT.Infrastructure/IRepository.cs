using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DoT.Infrastructure
{
    public interface IRepository<T>
    {
        Task<List<T>> ListAsync();
        Task<List<T>> ListAsync(ISpecification<T> spec);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<T> FirstOrDefaultAsync(ISpecification<T> spec);
        Task<T> SingleOrDefaultAsync(ISpecification<T> spec);
        Task AddRangeAsync(IEnumerable<T> range);
        Task<T> AddAsync(T item);
        List<T> FindBy(Expression<Func<T, bool>> predicate);
        Task<List<T>> FindByAsync(Expression<Func<T, bool>> predicate);
        T Create(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<List<T>> StoredProc(string procString, params object[] parameters);
    }
}