using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Users.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateRangeAsync(IEnumerable<User> users)
        {
            _context.Users.UpdateRange(users);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(id, cancellationToken);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<User>> FindByConditionAsync(Expression<Func<User, bool>> expression)
        {
            return await _context.Users.Where(expression).ToListAsync();
        }

        public async Task StoreRefreshTokenAsync(int userId, string token, DateTime expiry)
        {
            var rt = new RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiryDate = expiry,
                IsRevoked = false
            };
            _context.RefreshTokens.Add(rt);
            await _context.SaveChangesAsync();
        }

        public async Task<string?> GetRefreshTokenAsync(int userId)
        {
            var rt = await _context.RefreshTokens
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .OrderByDescending(x => x.ExpiryDate)
                .FirstOrDefaultAsync();
            return rt?.Token;
        }

        public async Task RevokeRefreshTokenAsync(int userId)
        {
            var tokens = _context.RefreshTokens.Where(x => x.UserId == userId && !x.IsRevoked);
            await tokens.ForEachAsync(x => x.IsRevoked = true);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUserIdByRefreshTokenAsync(string refreshToken)
        {
            var rt = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == refreshToken && !x.IsRevoked && x.ExpiryDate > DateTime.UtcNow);
            if (rt == null)
                throw new SecurityTokenException("Invalid or expired refresh token");
            return rt.UserId;
        }
    }
}
