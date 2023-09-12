using System.Linq;
using HotChocolate;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class Query
{
    public PlayerModel? GetPlayer([Service] ServiceContext serviceContext, string avatarAddress)
    {
        var query = serviceContext.Players.AsNoTracking().FirstOrDefault(p => p.AvatarAddress == new Address(avatarAddress));
        return query;
    }
}
