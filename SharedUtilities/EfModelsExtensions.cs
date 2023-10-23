using Ef_Models;
using Microsoft.EntityFrameworkCore;

namespace SharedUtilities;

public static class EfModelsExtensions
{
    public static Task<PersonalChat?> GetPersonalChat(this IQueryable<PersonalChat> source, int firstId, int secondId)
    {
        return source.FirstOrDefaultAsync(pc => (pc.FirstClient.Id == firstId && pc.SecondClientId == secondId) ||
                                         (pc.SecondClient.Id == secondId && pc.FirstClientId == firstId));
    }
}