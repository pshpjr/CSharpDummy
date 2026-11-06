using System.Net;
using System.Threading.Channels;
using CSharpDummy;
using CSharpDummy.NetLib;
using Microsoft.Extensions.Configuration;

public class Dummy
{
    private const int Tickcount = 100;
    private IConfigurationRoot _configuration;
    private List<DummyActor> _lstDummyActor = new();
    public Dummy(IConfigurationRoot configuration)
    {
        _configuration = configuration;
    }

    public async Task RunAsync(CancellationToken ctsToken)
    {
        int dummyCount = _configuration.GetValue<short>("Base:maxPlayer");
        int startAccount = _configuration.GetValue<short>("Base:startAccount");

        List<Task> tasks = new List<Task>(dummyCount + 1);

        var ipString = _configuration["Base:IP"]!;
        var port = _configuration.GetValue<short>("Base:Port");
        var endPoint = new IPEndPoint(IPAddress.Parse(ipString), port);
        
        Console.WriteLine($"{ipString}:{port}");
        
        for (int i = 0; i < dummyCount; i++)
        {
            DummyActor dummy = new DummyActor(endPoint, startAccount + i);
            _lstDummyActor.Add(dummy);

            tasks.Add(dummy.RunAsync(ctsToken));
        }
        
        using var tickCts = CancellationTokenSource.CreateLinkedTokenSource(ctsToken);
        var tickLoop = TickLoop(tickCts.Token);
        try
        {
            await Task.WhenAll(tasks);
        }
        finally
        {
            tickCts.Cancel();
            await tickLoop;
        }
    }
    
    async Task TickLoop(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Tickcount));

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                foreach (var dummyActor in _lstDummyActor)
                {
                    dummyActor.RequestTick();
                }
            }
        }
        catch (OperationCanceledException e)
        {
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

public class DummyActor : IPacketHandler
{
    private enum RunningState : byte { Idle, Running, Dirty }
    
    public readonly Channel<Action> Inbox = Channel.CreateUnbounded<Action>();
    private readonly IPEndPoint _endpoint;
    private int _runningState = (int)RunningState.Idle;
    public string AccountId;
    private string _accountPass;
    private Session _session;

    public int Id { get; set; }

    public DummyActor(IPEndPoint endPoint, int id)
    {
        _endpoint = endPoint;
        Id = id;
        AccountId = $"ID_{Id}";
        _accountPass = $"PASS_{Id}";
    }
    
    public async Task RunAsync(CancellationToken ctsToken)
    {
        _session = new Session(Deserialize)
        {
            OnDisconnect = () =>
            {
                Console.WriteLine($"[{AccountId}] disconnected");
                Inbox.Writer.TryComplete();
            }
        };
        await _session.ConnectAsync(_endpoint);
        Task networkLoop = _session.Run(ctsToken);

        var packet = SendPacket.Create();
        Protocol.MakeLogin_ReqLogin(packet, AccountId, _accountPass);
        await _session.Send(packet);
        
        //  세션 종료 콜백에서 닫는다.
        await foreach (var job in Inbox.Reader.ReadAllAsync())
        {
            try
            {
                job();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        await networkLoop;
    }
    
    public void Deserialize(PacketType type, PacketReader reader)
    {
        var packet = Protocol.Deserialize(type, ref reader);
        //  decode에서 패킷의 데이터를 전부 사용해야 함
        if (reader.Count != 0)
            throw new InvalidDataException();
        
        Post(() =>Protocol.Dispatch(packet, this));
    }


    public void RequestTick()
    {
        while (true)
        {
            var state = (RunningState)Volatile.Read(ref _runningState);
            if (state == RunningState.Dirty)
                return;

            var next = state == RunningState.Idle ? RunningState.Running : RunningState.Dirty;

            if (Interlocked.CompareExchange(ref _runningState, (int)next, (int)state) != (int)state)
                continue;
            
            //  오류: 내가 실행 권한 얻었는데 Tick 실행 하라고 못 넣었다. -> 권한을 놔준다.
            if (next == RunningState.Running && !Inbox.Writer.TryWrite(Tick))
                Interlocked.Exchange(ref _runningState, (int)RunningState.Idle);

            break;
        }
    }
    
    public void Tick()
    {
        try
        {
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        if (!ShouldResumeTick())
        {
            return;
        }
        
        //  일부러 맨 뒤에 넣어서 job도 처리하게 함. 
        if (!Inbox.Writer.TryWrite(Tick))
            Interlocked.Exchange(ref _runningState, (int)RunningState.Idle);
    }

    //  외부 요청이 있어서 틱을 내가 계속 돌려야 하는지
    private bool ShouldResumeTick()
    {
        while (true)
        {
            int state = Volatile.Read(ref _runningState);
            var next = state == 2 ? 1 : 0;

            if (Interlocked.CompareExchange(ref _runningState, next, state) == state)
                return next == 1;
        }
    }

    public void Post(Action action)
    {
        Inbox.Writer.TryWrite(action);
    }
    
    public void OnLogin_ResLogin(Login_ResLogin packet)
    {
        Console.WriteLine($"[{AccountId}] login {packet.Result}, accountNo={packet.AccountNo}");
        _ = _session.Disconnect();   // → OnDisconnect → Inbox 완료 → RunAsync 종료
    }

    public void OnLogin_ResRegister(Login_ResRegister packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResLogin(Game_ResLogin packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResLevelEnter(Game_ResLevelEnter packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResCreateActor(Game_ResCreateActor packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResChracterDetail(Game_ResChracterDetail packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResPlayerDetail(Game_ResPlayerDetail packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResDestroyActor(Game_ResDestroyActor packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResGetCoin(Game_ResGetCoin packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResMove(Game_ResMove packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResMoveStop(Game_ResMoveStop packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResAttack(Game_ResAttack packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResDraw(Game_ResDraw packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResHit(Game_ResHit packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResSpawnItem(Game_ResSpawnItem packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResDestroyItem(Game_ResDestroyItem packet)
    {
        throw new NotImplementedException();
    }

    public void OnGame_ResChat(Game_ResChat packet)
    {
        throw new NotImplementedException();
    }

    public void OnVersion_ResVersion(Version_ResVersion packet)
    {
        throw new NotImplementedException();
    }

    public void OnVersion_ResGameData(Version_ResGameData packet)
    {
        throw new NotImplementedException();
    }
}
