using Microsoft.Extensions.Caching.Memory;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GPTW.ListAutomation.Core.Services.Caching;

/// <summary>
/// Represents a manager for caching
/// </summary>
public partial class MemoryCacheManager : ICacheManager
{
    static MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheManager"/> class.
    public MemoryCacheManager()
    {
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    /// <param name="key">The key of the value to get.</param>
    /// <returns>The value associated with the specified key.</returns>
    public virtual T Get<T>(string key)
    {
        object val = null;
        if (key != null && Cache.TryGetValue(key, out val))
        {
            return (T)val;
        }
        else
        {
            return (T)default(object);
        }
    }

    /// <summary>
    /// Adds the specified key and object to the cache.
    /// </summary>
    /// <param name="key">key</param>
    /// <param name="data">Data</param>
    /// <param name="cacheTime">Cache time</param>
    public virtual void Set(string key, object data, int cacheTime)
    {
        if (data == null)
            return;

        if (key != null)
        {
            Cache.Set(key, data, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(cacheTime)
            });
        }
    }

    /// <summary>
    /// Gets a value indicating whether the value associated with the specified key is cached
    /// </summary>
    /// <param name="key">key</param>
    /// <returns>Result</returns>
    public virtual bool IsSet(string key)
    {
        object val = null;
        if (key != null && Cache.TryGetValue(key, out val))
        {
            if (val != null)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Removes the value with the specified key from the cache
    /// </summary>
    /// <param name="key">/key</param>
    public virtual void Remove(string key)
    {
        Cache.Remove(key);
    }

    /// <summary>
    /// Removes items by pattern
    /// </summary>
    /// <param name="pattern">pattern</param>
    public virtual void RemoveByPattern(string pattern)
    {
        var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var keysToRemove = new List<String>();

        foreach (var item in GetCacheKeys())
            if (regex.IsMatch(item))
                keysToRemove.Add(item);

        foreach (string key in keysToRemove)
        {
            Remove(key);
        }
    }

    /// <summary>
    /// Clear all cache data
    /// </summary>
    public virtual void Clear()
    {
        foreach (var item in GetCacheKeys())
            Remove(item);
    }

    private List<string> GetCacheKeys()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var entries = Cache.GetType().GetField("_entries", flags).GetValue(Cache);
        var cacheItems = entries as IDictionary;
        var keys = new List<string>();
        if (cacheItems == null) return keys;
        foreach (DictionaryEntry cacheItem in cacheItems)
        {
            var key = cacheItem.Key.ToString();

            keys.Add(key);
        }
        return keys;
    }

    /// <summary>
    /// Removes the value with the specified key from the cache directly
    /// </summary>
    /// <param name="key">key</param>
    public void DirectRemove(string key)
    {
        Cache.Remove(key);
    }
}