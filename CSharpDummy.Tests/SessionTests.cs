using System.Net;
using System.Net.Sockets;
using CSharpDummy.NetLib;

namespace CSharpDummy.Tests;

public class SessionTests
{
    [Fact]
    public async Task ServerClose_OnDisconnect_Called()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        bool disconnected = false;
        var session = new Session((_, _) => { }) { OnDisconnect = () => disconnected = true };

        var accept = listener.AcceptSocketAsync();
        await session.ConnectAsync((IPEndPoint)listener.LocalEndpoint);
        (await accept).Close();

        await session.Run(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
        Assert.True(disconnected);
    }
}
