using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace eforms_middleware.Services
{
    public class EmployeeDetailsService
    {
        private readonly ILogger _log;
        private readonly IConfiguration _configuration;
        private readonly TokenService _tokenService;
        private readonly IRepository<AdfUser> _adfUserEntity;

        public EmployeeDetailsService(ILogger log, IConfiguration configuration, TokenService tokenService, IRepository<AdfUser> adfUser)
        {
            _log = log;
            _configuration = configuration;
            _tokenService = tokenService;
            _adfUserEntity = adfUser;
        }

        public async Task<List<AdfUser>> GetEmployee()
        {
            var data = await _adfUserEntity.ListAsync();
            return data;
        }
    }
}