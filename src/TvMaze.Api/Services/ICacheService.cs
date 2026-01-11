namespace TvMaze.Api.Services;

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? absoluteExpiration = null) where T : class;
    Task<T> GetOrCreateValueAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null) where T : struct;
    void Remove(string key);
    void RemoveByPrefix(string prefix);
    void Clear();
}
