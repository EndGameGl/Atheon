namespace Atheon.Services.Interfaces;

public interface ISettingsStorage
{
    Task<T?> GetOption<T>(string key);
    Task SetOption<T>(string key, T value);
}
