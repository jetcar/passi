using System;

namespace Models
{
    public class ClientException : Exception
    {
        public ClientException(string message) : base(message)
        {
        }
    }
}