namespace TheCabin.Core.Interfaces;

/// <summary>
/// Interface for managing application preferences and settings
/// </summary>
public interface IPreferencesService
{
    /// <summary>
    /// Gets a boolean preference value
    /// </summary>
    bool Get(string key, bool defaultValue);
    
    /// <summary>
    /// Gets a double preference value
    /// </summary>
    double Get(string key, double defaultValue);
    
    /// <summary>
    /// Gets a string preference value
    /// </summary>
    string Get(string key, string defaultValue);
    
    /// <summary>
    /// Sets a boolean preference value
    /// </summary>
    void Set(string key, bool value);
    
    /// <summary>
    /// Sets a double preference value
    /// </summary>
    void Set(string key, double value);
    
    /// <summary>
    /// Sets a string preference value
    /// </summary>
    void Set(string key, string value);
    
    /// <summary>
    /// Clears all preferences
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Removes a specific preference
    /// </summary>
    void Remove(string key);
    
    /// <summary>
    /// Checks if a preference key exists
    /// </summary>
    bool ContainsKey(string key);
}
