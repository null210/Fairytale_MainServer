using MainServer.Models;

namespace MainServer.Service
{
    public interface IUserService
    {
        Task<Models.User> AuthenticateGoogleUser(string idToken);
        Task<Models.User> GetOrCreateUserByDevice(string deviceId);
        Task<Models.User> GetUserById(string userId);
        Task<Models.User> UpdateUser(Models.User user);
    }
}
