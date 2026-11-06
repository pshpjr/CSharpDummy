namespace CSharpDummy;

public sealed record BaseOptions(
        string IP,
        int Port, 
        bool UseDB, 
        int StartAccount,
        int MaxPlayer, 
        int PlayerPerGroup, 
        int ReconnectTime
    );

public sealed record PermilOptions();

public sealed record IntervalOptions();

public sealed record SessionOptions();
