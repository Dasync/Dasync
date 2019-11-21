using System;

namespace Users.Contract
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException()
            : base("The user could not be found")
        {
            HResult = 46;
        }
    }
}
