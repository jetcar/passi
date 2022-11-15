using System;
using System.Collections.Generic;

namespace IdentityServer.Controllers.ClientRegistration
{
    public class ClientView
    {
        public List<ClientItem> ClientItems { get; set; } = new List<ClientItem>();
    }

    public class ClientItem
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public DateTime CreatedTime { get; set; }
        public string Enabled { get; set; }
        public string ReturnUrl { get; set; }
    }
}