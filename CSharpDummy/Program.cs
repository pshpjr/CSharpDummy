using System;
using System.Runtime.InteropServices;
using CSharpDummy.NetLib;
using Microsoft.Extensions.Configuration;

class Program
{
    
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddTomlFile("DummySetting.toml")
            .AddEnvironmentVariables("DUMMY_")
            .Build();

        using var cts = new CancellationTokenSource();

        void SigHandler(PosixSignalContext signal)
        {
            Console.WriteLine($"Received signal: {signal}");
            signal.Cancel = true;
            cts.Cancel();
        }
        
        using var sigint =  PosixSignalRegistration.Create(PosixSignal.SIGINT, SigHandler);
        using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, SigHandler);

        Dummy dummy = new Dummy(configuration);
        
        await dummy.RunAsync(cts.Token);
       
    }
    

    
}
