using System;
using System.Threading.Tasks;
using Users.Contract;

namespace AntiFraud.Domain
{
    // This service does not have a contract since it just reacts to events.
    public class AntiFraudService
    {
        private readonly IUsersService _usersService;

        public AntiFraudService(IUsersService usersService)
        {
            _usersService = usersService;

            // Subscribe to the event of another service
            _usersService.UserRegistered += OnUserRegistered;
        }

        protected virtual async void OnUserRegistered(object sender, User user)
        {
            await VerifyUser(user);
        }

        protected virtual async Task VerifyUser(User user)
        {
            if (IsSuspiciousEmailAddress(user.Email))
            {
                // Send command to another service and wait for the response.
                await _usersService.SuspendUser(user.Email);
            }
        }

        private bool IsSuspiciousEmailAddress(string email)
        {
            // TODO: improve the ML model
            return Environment.TickCount % 1000 > 750;
        }
    }
}
