using Microsoft.AspNetCore.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Me.Chats;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;

namespace TestTeamsApp;

public class GraphClient : IAccessTokenProvider
{
    private readonly string _token;

    public GraphClient(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentNullException(nameof(token));
        }

        _token = token;
    }

    public AllowedHostsValidator AllowedHostsValidator => throw new NotImplementedException();

    public async Task<List<ChatMessage>> GetChatMessagesAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var client = GetAuthenticatedClient();
        var messageResponse = await client.Me.Chats[conversationId].Messages.GetAsync(cancellationToken: cancellationToken);
        return messageResponse.Value;
    }

    public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_token);
    }

    private GraphServiceClient GetAuthenticatedClient()
    {
        return new GraphServiceClient(new BaseBearerTokenAuthenticationProvider(this));
    }
}