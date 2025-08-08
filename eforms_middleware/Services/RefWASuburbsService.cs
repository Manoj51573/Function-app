using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace eforms_middleware.Services;

public class RefWASuburbsService
{
    private readonly ILogger _log;
    private readonly IConfiguration _configuration;
    private readonly TokenService _tokenService;
    private readonly IRepository<RefWASuburb> _ncLocationEntity;

    public RefWASuburbsService(ILogger log, IConfiguration configuration, TokenService tokenService, IRepository<RefWASuburb> refWASuburbEntity)
    {
        _log = log;
        _configuration = configuration;
        _tokenService = tokenService;
        _ncLocationEntity = refWASuburbEntity;
    }

    public async Task<List<RefWASuburb>> GetAllLocationTask()
    {
        var data = await _ncLocationEntity.ListAsync();
        return data;
    }
}