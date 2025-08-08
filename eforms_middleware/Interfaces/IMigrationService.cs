using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.DataModel;
using Microsoft.EntityFrameworkCore.Migrations;

namespace eforms_middleware.Interfaces;

public interface IMigrationService
{
    Task MigrateBlobsAsync(List<MigrationModel> blobs);
}