using Microsoft.Maui.Storage;
using TheCabin.Core.Interfaces;

namespace TheCabin.Maui.Services;

/// <summary>
/// MAUI implementation of preferences service that wraps platform-specific preferences
/// </summary>
public class MauiPreferencesService : IPreferencesService
{
    public bool Get(string key, bool defaultValue)
    {
        return Preferences.Get(key, defaultValue);
    }

    public double Get(string key, double defaultValue)
    {
        return Preferences.Get(key, defaultValue);
    }

    public string Get(string key, string defaultValue)
    {
        return Preferences.Get(key, defaultValue);
    }

    public void Set(string key, bool value)
    {
        Preferences.Set(key, value);
    }

    public void Set(string key, double value)
    {
        Preferences.Set(key, value);
    }

    public void Set(string key, string value)
    {
        Preferences.Set(key, value);
    }

    public void Clear()
    {
        Preferences.Clear();
    }

    public void Remove(string key)
    {
        Preferences.Remove(key);
    }

    public bool ContainsKey(string key)
    {
        return Preferences.ContainsKey(key);
    }
}
