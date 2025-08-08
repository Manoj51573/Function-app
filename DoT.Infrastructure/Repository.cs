using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;

namespace DoT.Infrastructure
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<Repository<T>> _logger;

        public Repository(AppDbContext dbContext, ILogger<Repository<T>> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<T>> ListAsync()
        {
            return await _dbContext.Set<T>().AsNoTracking().ToListAsync();
        }

        public async Task<List<T>> ListAsync(ISpecification<T> spec)
        {
            return await ListInternal(spec).ToListAsync();
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbContext.Set<T>().FirstOrDefaultAsync(predicate);
        }

        public async Task<T> FirstOrDefaultAsync(ISpecification<T> spec)
        {
            return await ListInternal(spec).FirstOrDefaultAsync();
        }

        public async Task<T> SingleOrDefaultAsync(ISpecification<T> spec)
        {
            return await ListInternal(spec).SingleOrDefaultAsync();
        }

        public List<T> FindBy(Expression<Func<T, bool>> predicate)
        {
            var result = _dbContext.Set<T>().AsNoTracking().AsQueryable();
            return result.Where(predicate).AsNoTracking().ToList();
        }

        public async Task<List<T>> FindByAsync(Expression<Func<T, bool>> predicate)
        {
            var result = _dbContext.Set<T>().AsNoTracking().AsQueryable();
            return await result.Where(predicate).AsNoTracking().ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<T> range)
        {
            await _dbContext.AddRangeAsync(range);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<T> AddAsync(T item)
        {
            await _dbContext.AddAsync(item);
            await _dbContext.SaveChangesAsync();
            return item;
        }

        public T Create(T entity)
        {
            try
            {
                _dbContext.Add(entity);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IRepository Error");
                throw;
            }
            return entity;
        }

        public void Update(T entity)
        {
            try
            {
                _dbContext.Update(entity);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IRepository Error");
                throw;
            }
        }

        public void Delete(T entity)
        {
            try
            {
                _dbContext.Entry(entity).State = EntityState.Deleted;
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IRepository Error");
                throw;
            }
        }

        protected IQueryable<T> ListInternal(ISpecification<T> spec)
        {
            var result = _dbContext.Set<T>().AsQueryable();
            result = spec.Includes.Aggregate(result, (current, include) => current.Include(include));
            result = spec.IncludeStrings.Aggregate(result, (current, include) => current.Include(include));

            if (spec.Criteria != null)
            {
                result = result.Where(spec.Criteria);
            }


            result = AddOrderBy(result, spec);
            result = result.Skip(spec.Skip ?? 0);
            result = spec.Take.HasValue ? result.Take(spec.Take.Value) : result;
            if (spec.Includes.Count > 0)
            {
                result = result.AsSplitQuery();
            }
            return result;
        }

        protected IQueryable<T> AddOrderBy(IQueryable<T> whereResult, ISpecification<T> spec)
        {
            if (spec.OrderBy != null)
            {
                whereResult = spec.OrderDescending ? whereResult.OrderByDescending(spec.OrderBy) : whereResult.OrderBy(spec.OrderBy);
            }

            return whereResult;
        }

        public async Task<List<T>> StoredProc(string procString
            , params object[] parameters)
        {
            return await _dbContext.Set<T>().FromSqlRaw($"EXECUTE {procString}", parameters).ToListAsync();
        }
    }
}