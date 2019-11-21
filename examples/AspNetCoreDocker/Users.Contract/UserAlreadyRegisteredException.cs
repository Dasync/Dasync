using System;

namespace Users.Contract
{
    public class UserAlreadyRegisteredException : Exception
    {
        public UserAlreadyRegisteredException()
            : base("The user is already registered")
        {
            HResult = 45;
        }
    }
}
