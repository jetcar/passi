using System;
using System.Collections.Generic;

namespace WebApiDto.Auth
{
    public class LoginResponceDto
    {
        public Guid SessionId { get; set; }
        public List<string> RegisteredDevices { get; set; } = [];
    }
}