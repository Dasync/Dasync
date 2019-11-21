using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Users.Contract
{
    // A service contract.
    public interface IUsersService
    {
        // A query. Queries don't change state.
        Task<List<User>> GetActiveUsers(int? top = null);

        // A command. Commands change state and can run both synchronously and asynchronously.
        Task RegisterUser(string name, string email);

        // Yet another command.
        Task SuspendUser(string email);

        // An event. Events invert service dependencies. Other services can react to it.
        event EventHandler<User> UserRegistered;
    }
}
