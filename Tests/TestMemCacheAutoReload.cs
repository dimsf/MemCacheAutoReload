using Dimf.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class TestMemCacheAutoReload
    {
        [TestMethod]
        public void TestGetOrAddAutoReload()
        {
            IMemoryCache memCache = new MemoryCache(new MemoryCacheOptions());

            string result = memCache.GetOrAddAutoReload("testKey", () =>
            {
                return "testValue";
            }, TimeSpan.FromSeconds(10));

            Assert.AreEqual("testValue", result);
        }

        [TestMethod]
        public void TestRemoveAutoReload()
        {
            IMemoryCache memCache = new MemoryCache(new MemoryCacheOptions());
            memCache.RemoveAutoReload("testKey");
            Assert.AreEqual(memCache.Get<string>("testKey"), null);
        }

        [TestMethod]
        public void TestGetOrAddAutoReloadAsync()
        {
            IMemoryCache memCache = new MemoryCache(new MemoryCacheOptions());

            Task<string> task1 = Task.Run(async () =>
            {
                return await memCache.GetOrAddAutoReloadAsync("testKey", async () =>
                {
                    return "task1Result";
                }, TimeSpan.FromMinutes(60));
            });

            task1.Wait();

            Task<string> task2 = Task.Run(async () =>
            {
                return await memCache.GetOrAddAutoReloadAsync("testKey", async () =>
                {
                    return "task2Result";
                }, TimeSpan.FromMinutes(60));
            });

            Task.WaitAll(task1, task2);
            
            Assert.AreEqual("task1Result", task2.Result);
        }

        [TestMethod]
        public async Task TestGetOrAddAutoReloadAsyncExpire()
        {
            IMemoryCache memCache = new MemoryCache(new MemoryCacheOptions());
            var result = await memCache.GetOrAddAutoReloadAsync("testKey", async () =>
            {
                return "result1";
            }, TimeSpan.FromSeconds(1));

            Assert.AreEqual("result1", result);
            await Task.Delay(1200);

            var result2 = await memCache.GetOrAddAutoReloadAsync("testKey", async () =>
            {
                return "result2";
            }, TimeSpan.FromSeconds(50));

            Assert.AreEqual("result2", result2);
        }
    }
}
