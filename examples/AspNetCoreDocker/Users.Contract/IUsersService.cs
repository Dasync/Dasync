using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Users.Contract
{
    // A service contract. The service name is 'Users' be the convention that discards the prefix 'I' and suffix "Service".
    public interface IUsersService
    {
        // A query. Queries don't change state.
        // To invoke this method, do HTTP GET to http://localhost:52979/api/Users/GetActiveUsers?top=10
        Task<List<User>> GetActiveUsers(int? top = null);

        // A command. Commands change state and can run both synchronously and asynchronously.
        // To invoke this method, do HTTP POST to http://localhost:52979/api/Users/RegisterUser with application/json body:
        // { "name": "test", "email": "test@dasync.io" }
        Task RegisterUser(string name, string email);

        // Yet another command.
        // To invoke this method, do HTTP POST to http://localhost:52979/api/Users/SuspendUser with application/json body:
        // { "email": "demo@dasync.io" }
        Task SuspendUser(string email);

        // An event. Events invert service dependencies. Other services can react to it.
        event EventHandler<User> UserRegistered;
    }
}
