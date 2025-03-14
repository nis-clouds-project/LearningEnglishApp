namespace Backend.Integrations.Interfaces
{
    public interface IYandexTokenService
    {
        Task<string> GetIamTokenAsync();
        string GetFolderId();
        Task StartTokenRefreshAsync(CancellationToken cancellationToken);
        Task RefreshTokenAsync();
    }
}