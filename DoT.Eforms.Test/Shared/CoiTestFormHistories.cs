using System;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;

namespace DoT.Eforms.Test.Shared;

public static class CoiTestFormHistories
{
    public static FormHistory RejectedByManager => new()
    {
        ActionType = Enum.GetName(FormStatus.Rejected), ActionBy = "ToManager@email"
    };
    
    public static FormHistory RejectebByTier3History => new()
    {
        ActionType = Enum.GetName(FormStatus.Rejected), ActionBy = "Tier3@email"
    };
}