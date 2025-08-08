using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.DataModel;

namespace eforms_middleware.Interfaces;

public interface IMessageFactoryService
{
    Task SendEmailAsync(FormInfo dbRecord, FormInfoUpdate request);
}