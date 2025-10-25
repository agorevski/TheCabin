using TheCabin.Core.Interfaces;

namespace TheCabin.Maui.Tests.Mocks;

/// <summary>
/// Mock implementation of preferences service for testing
/// </summary>
public class MockPreferencesService : IPreferencesService
{
    private readonly Dictionary<string, object> _storage = new();

    public bool Get(string key, bool defaultValue)
    {
        if (_storage.TryGetValue(key, out var value) && value is bool boolValue)
        {
            return boolValue;
        }
        return defaultValue;
    }

    public double Get(string key, double defaultValue)
    {
        if (_storage.TryGetValue(key, out var value) && value is double doubleValue)
        {
            return doubleValue;
        }
        return defaultValue;
    }

    public string Get(string key, string defaultValue)
    {
        if (_storage.TryGetValue(key, out var value) && value is string stringValue)
        {
            return stringValue;
        }
        return defaultValue;
    }

    public void Set(string key, bool value)
    {
        _storage[key] = value;
    }

    public void Set(string key, double value)
    {
        _storage[key] = value;
    }

    public void Set(string key, string value)
    {
        _storage[key] = value;
    }

    public void Clear()
    {
        _storage.Clear();
    }

    public void Remove(string key)
    {
        _storage.Remove(key);
    }

    public bool ContainsKey(string key)
    {
        return _storage.ContainsKey(key);
    }
}
