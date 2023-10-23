using Ef_Models;
using Microsoft.EntityFrameworkCore;

namespace SharedUtilities;

public static class EfModelsExtensions
{
    public static Task<PersonalChat?> GetPersonalChatAsync(this IQueryable<PersonalChat> source, int firstId, int secondId)
    {
        return source.FirstOrDefaultAsync(pc => (pc.FirstClient.Id == firstId && pc.SecondClientId == secondId) ||
                                         (pc.SecondClient.Id == secondId && pc.FirstClientId == firstId));
    }
    public static Task<PersonalChat?> GetPersonalChatAsync(this IQueryable<PersonalChat> source, string firstUsername, string secondUsername)
    {
        return source.FirstOrDefaultAsync(chat =>
            (chat.FirstClient.Username == firstUsername && chat.SecondClient.Username == secondUsername) ||
            (chat.FirstClient.Username == secondUsername && chat.SecondClient.Username == firstUsername));
    }
}