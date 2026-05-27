using CoreMVC.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreMVC.Infrastructure.Services
{
    public class TokenCacheService : ITokenCacheService
    {
        private readonly IDistributedCache _cache;

        public TokenCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        private static string Key(string userId) => $"access_token:{userId}";

        public async Task<string?> GetAccessTokenAsync(string userId)
        {
            return await _cache.GetStringAsync(Key(userId));
        }

        public async Task SetAccessTokenAsync(string userId, string token, int expiresInSeconds)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(expiresInSeconds)
            };
            await _cache.SetStringAsync(Key(userId), token, options);
        }

        public async Task RemoveAccessTokenAsync(string userId)
        {
            await _cache.RemoveAsync(Key(userId));
        }
    }
}