namespace Sesame.Web.Services
{
    public interface ISessionStateService
    {
        T Get<T>(string key);

        void Set<T>(string key, T value);
    }
}