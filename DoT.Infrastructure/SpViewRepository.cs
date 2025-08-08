using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DoT.Infrastructure;

public class SpViewRepository<T> : ISpViewRepository<T> where T : class
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SpViewRepository<T>> _logger;

    public SpViewRepository(AppDbContext dbContext, ILogger<SpViewRepository<T>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<T>> ToListAsync(string sqlCommand, params object[] parameters)
    {
        return await _dbContext.Set<T>().FromSqlRaw(sqlCommand, parameters).ToListAsync();
    }

    public async Task<T> SingleOrDefaultAsync(string sqlCommand, params object[] parameters)
    {
        var list = await _dbContext.Set<T>().FromSqlRaw(sqlCommand, parameters).ToListAsync();
        return list.AsEnumerable().SingleOrDefault();
    }
}