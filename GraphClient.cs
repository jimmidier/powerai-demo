using Microsoft.Graph;
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

    public async Task<List<ChatMessage>> GetChatMessagesAsync(string conversationId, CancellationToken cancellationToken = default, int top = 5)
    {
        var client = GetAuthenticatedClient();

        var messageResponse = await client.Me.Chats[conversationId].Messages.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Top = top;
        }, cancellationToken);
        return messageResponse.Value;
    }

    public async Task<ChatMessage> GetSingleChatMessagesAsync(string conversationId, string messageId, CancellationToken cancellationToken = default)
    {
        var client = GetAuthenticatedClient();

        var messageResponse = await client.Me.Chats[conversationId].Messages[messageId].GetAsync(null, cancellationToken);
        return messageResponse;
    }

    public async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var user = await client.Me.GetAsync(cancellationToken: cancellationToken);
            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取用户信息时出错: {ex.Message}");
            return null;
        }
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