using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Dimf.Extensions.Caching.Memory
{
    public static class MemoryCacheExtensions
    {
        private static SemaphoreSlim lockObj = new SemaphoreSlim(1, 1);
        private const string CachedValueKeyFormat = "autorefresh_{0}_cached";

        /// <summary>
        /// Removes the specified key from cache.
        /// </summary>
        public static void RemoveAutoReload(this IMemoryCache memCache, string key)
        {
            memCache.Remove(key);
            memCache.Remove(GetCacheKey(key));
        }

        /// <summary>
        /// Adds the specified value returns by the value provider.
        /// </summary>
        public static void SetAutoReload<T>(this IMemoryCache memCache, string key, Func<T> valueProvider, TimeSpan refreshInterval)
        {
            lockObj.Wait();

            try
            {
                memCache.SetNewValue(key, valueProvider(), refreshInterval);

            }
            catch
            {
                throw;
            }
            finally
            {
                lockObj.Release();
            }
        }

        /// <summary>
        /// Adds the specified value returns by the value provider.
        /// </summary>
        public static async Task SetAutoReloadAsync<T>(this IMemoryCache memCache, string key, Func<Task<T>> valueProvider, TimeSpan refreshInterval)
        {
            await lockObj.WaitAsync();
            
            try
            {
                var result = await valueProvider();
                memCache.SetNewValue(key, result, refreshInterval);
            }
            catch
            {
                throw;
            }
            finally
            {
                lockObj.Release();
            }
        }

        /// <summary>
        /// Executes the specified value provider and stores the result in cache if it does not exist.
        /// The value provider is executed only once. If the value exists it is returned.
        /// </summary>
        public static T GetOrAddAutoReload<T>(this IMemoryCache memCache, string key, Func<T> valueProvider, TimeSpan refreshInterval)
        {
            if (memCache.GetExistingValue(key, out T value))
            {
                return value;
            }

            //Neither cached entry found.

            lockObj.Wait();

            if (!memCache.TryGetValue(key, out value))
            {
                try
                {
                    value = valueProvider();
                    memCache.SetNewValue(key, value, refreshInterval);
                }
                catch
                {
                    lockObj.Release();
                    throw;
                }
                finally
                {
                    lockObj.Release();
                }
            }

            return value;
        }

        /// <summary>
        /// Executes the specified value provider and stores the result in cache if it does not exist.
        /// The value provider is executed only once. If the value exists it is returned.
        /// </summary>
        public static async Task<T> GetOrAddAutoReloadAsync<T>(this IMemoryCache memCache, string key, Func<Task<T>> valueProvider, TimeSpan refreshInterval)
        {
            if (memCache.GetExistingValue(key, out T value))
            {
                return value;
            }

            //Neither cached entry found.
            await lockObj.WaitAsync();

            if (!memCache.TryGetValue(key, out value))
            {
                try
                {
                    value = await valueProvider();
                    memCache.SetNewValue(key, value, refreshInterval);
                }
                catch
                {
                    lockObj.Release();
                    throw;
                }
                finally
                {
                    lockObj.Release();
                }
            }

            return value;
        }

        private static bool GetExistingValue<T>(this IMemoryCache memCache, string key, out T value)
        {
            bool found = false;
            value = default;

            if (memCache.TryGetValue(key, out T currValue))
            {
                found = true;
                value = currValue;
            }

            //Key not exists. Search in cached entry.
            bool hasCachedValue = memCache.TryGetValue(GetCacheKey(key), out T oldValue);
            if (hasCachedValue && lockObj.CurrentCount == 0)
            {
                found = true;
                value = oldValue;
            }

            return found;
        }

        private static void SetNewValue<T>(this IMemoryCache memCache, string key, T value, TimeSpan refreshInterval)
        {
            memCache.Set(key, value, refreshInterval);
            memCache.Set(GetCacheKey(key), value);
        }

        private static string GetCacheKey(string key)
        {
            return string.Format(CachedValueKeyFormat, key);
        }
    }
}

