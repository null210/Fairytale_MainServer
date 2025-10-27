using Google;
using MainServer.Data;
using MainServer.Models;
using Microsoft.EntityFrameworkCore;

namespace MainServer.Service
{
    public class UserService : IUserService
    {
        private readonly MainServerDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(MainServerDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User> AuthenticateGoogleUser(string idToken)
        {
            try
            {
                // Google OAuth 검증 로직
                // Google.Apis.Auth 패키지 사용
                // var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);

                // 임시 구현 (실제 구현시 Google OAuth 검증 필요)
                var googleId = "google_" + Guid.NewGuid().ToString();
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == googleId);

                if (user == null)
                {
                    user = new User
                    {
                        Id = googleId,
                        Email = "user@example.com", // payload.Email
                        Name = "사용자", // payload.Name
                        CreatedAt = DateTime.Now,
                        LastLoginAt = DateTime.Now
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"New user created: {user.Id}");
                }
                else
                {
                    user.LastLoginAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating Google user");
                return null;
            }
        }

        public async Task<User> GetOrCreateUserByDevice(string deviceId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DeviceId == deviceId);

            if (user == null)
            {
                user = new User
                {
                    Id = "device_" + Guid.NewGuid().ToString(),
                    DeviceId = deviceId,
                    Name = "Guest",
                    CreatedAt = DateTime.Now,
                    LastLoginAt = DateTime.Now
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"New device user created: {user.Id}");
            }
            else
            {
                user.LastLoginAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return user;
        }

        public async Task<User> GetUserById(string userId)
        {
            return await _context.Users
                .Include(u => u.Stories)
                .Include(u => u.UserTags)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User> UpdateUser(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}
