# MemCacheAutoReload
Extension methods for ASP.NET Core MemoryCache, to support automatic, thread-safe lazy initialization of cache entries.

# Usage
#### Add or get entry(async version)

```csharp
Task<T> GetOrAddAutoReloadAsync<T>(string key, Func<Task<T>> valueProvider, TimeSpan refreshInterval)
```

#### Example
```csharp
IMemoryCache memCache = ...;
var result = await memCache.GetOrAddAutoReloadAsync("key", async() => 
{
    await Task.Delay(2000); //Simulate some delay
    return Environment.TickCount; //Return result
}, TimeSpan.FromSeconds(2)); //Refresh(call value provider every two seconds)
```

#### Add or get entry(sync version)
```csharp
T GetOrAddAutoReloadAsync<T>(string key, Action<T> valueProvider, TimeSpan refreshInterval)
```

```csharp
IMemoryCache memCache = ...;
var result = memCache.GetOrAddAutoReloadAsync("key", () => 
{
        Thread.Sleep(2000); //Simulate some delay
        return Environment.TickCount; //Return result
}, TimeSpan.FromSeconds(2)); //Refresh(call value provider every two seconds)
```

The GetOrAddAutoReaload/GetOrAddAutoReloadAsync are thread safe. The value generated is cached, and only one thread at a time executes the value provider. The cached value is returned in case a thread executes the value provider to refresh the current value, so there is no blocking except the first time, where no cached value if available.

#### Set value
````csharp
memCache.SetAutoReload("testKey", () = > "a test value", TimeSpan.FromSeconds(2));
````

#### Remove value
````csharp
memCache.RemoveAutoReload("testKey");
````
