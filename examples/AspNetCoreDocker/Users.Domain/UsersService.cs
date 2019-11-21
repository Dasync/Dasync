using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Users.Contract;

namespace Users.Domain
{
    // Domain-specific business logic of a service, should not contain any infrastructural aspects.
    public class UsersService : IUsersService
    {
        // Should be a DB-backed repository, but keep the list in-memory just for the demo's sake.
        private readonly List<User> _users = new List<User>
        {
            new User
            {
                Name = "Example Maker",
                Email = "demo@dasync.io"
            }
        };

        public virtual async Task<List<User>> GetActiveUsers(int? top = null)
        {
            return _users.Where(u => !u.IsSuspended).Take(top ?? 10).ToList();
        }

        public virtual async Task RegisterUser(string name, string email)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email));

            if (_users.Any(u => u.Email == email))
                throw new UserAlreadyRegisteredException();

            var newUser = new User
            {
                Name = name,
                Email = email
            };

            _users.Add(newUser);

            // Publish the event so other services can react to it.
            UserRegistered?.Invoke(this, newUser);
        }

        public virtual async Task SuspendUser(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email));

            var user = _users.FirstOrDefault(u => u.Email == email);

            if (user == null)
                throw new UserNotFoundException();

            user.IsSuspended = true;
        }

        public virtual event EventHandler<User> UserRegistered;
    }
}
