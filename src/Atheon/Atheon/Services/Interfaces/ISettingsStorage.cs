namespace Atheon.Services.Interfaces;

public interface ISettingsStorage
{
    Task<T?> GetOption<T>(string key, Func<T>? defaultValue = null);
    Task SetOption<T>(string key, T value);
}
