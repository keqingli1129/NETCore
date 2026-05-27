using System;
using System.Collections.Generic;
using System.Text;

namespace CoreMVC.Application.Interfaces
{
    public interface ITokenCacheService
    {
        Task<string?> GetAccessTokenAsync(string userId);
        Task SetAccessTokenAsync(string userId, string token, int expiresInSeconds);
        Task RemoveAccessTokenAsync(string userId);
    }
}
