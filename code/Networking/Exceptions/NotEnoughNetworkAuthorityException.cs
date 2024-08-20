using Sandbox;
using System;

namespace Mini.Networking.Exceptions;

public sealed class NotEnoughNetworkAuthorityException : Exception
{
    public NotEnoughNetworkAuthorityException(string message) : base(message)
    {

    }

    public static void ThrowIfLocalIsNotHost()
    {
        if(!Connection.Local.IsHost)
            throw new NotEnoughNetworkAuthorityException("Local connection is not host.");
    }
}
