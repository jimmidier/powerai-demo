using IntelR.Shared;
using Refit;

namespace TestTeamsApp.RemoteApis;

public interface IIntelRApi
{
    [Post("/api/reply/registerconversation")]
    Task<string> RegisterConversationAsync(GenerateReplyRequest request);
}