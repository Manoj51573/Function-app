using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace eforms_middleware.Services;

public class NcLocationService
{
    private readonly ILogger _log;
    private readonly IConfiguration _configuration;
    private readonly TokenService _tokenService;
    private readonly IRepository<NCLocation> _ncLocationEntity;

    public NcLocationService(ILogger log, IConfiguration configuration, TokenService tokenService, IRepository<NCLocation> ncLocationEntity)
    {
        _log = log;
        _configuration = configuration;
        _tokenService = tokenService;
        _ncLocationEntity = ncLocationEntity;
    }

    public async Task<List<NCLocation>> GetAllLocationTask()
    {
        var data = await _ncLocationEntity.ListAsync();
        return data;
    }
}