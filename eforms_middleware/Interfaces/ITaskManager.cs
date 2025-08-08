using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Interfaces;

public interface ITaskManager
{
    Task<IList<TaskInfo>> GetTaskInfoAsync(ISpecification<TaskInfo> specification);
    Task AddFormTaskAsync(int formInfoId, TaskInfo taskInfo = null);
    Task AddFormTaskAsync(TaskInfo originalTask, TaskInfo taskInfo = null);
}