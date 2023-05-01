using System.Net;

namespace AS_Chat.Core;


public class SessionContext
{
    public string? Username { get; set; }
    public IPEndPoint? ServerIpEndPoint { get; set; }
}