namespace Sesame.Web.Contracts
{
    public interface ISessionStateService
    {
        T Get<T>(string key);

        void Set<T>(string key, T value);
    }
}