/*
 * ServerStressTester.cs
 * 서버 부하 테스트
 * 
 * 가짜 클라이언트를 생성하여 접속을 시도함
 */
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models;
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.Interfaces;
using Gomoku.Models.Messages;
using Gomoku.Models.Network;
using Gomoku.Services.Applications;
using Gomoku.Services.Applications.Auth;
using Gomoku.Services.Applications.Command;
using Gomoku.Services.Applications.Database;
using Gomoku.Services.Applications.Game;
using Gomoku.Services.Applications.Request;
using Gomoku.Services.Wpf;
using Gomoku.Services.Wpf.Dialogs;
using Gomoku.Services.Wpf.Media;
using Gomoku.Services.Wpf.Window;
using Microsoft.Extensions.DependencyInjection;


internal class Program
{
    private static async Task Main(string[] args)
    {
        var services = new ServiceCollection(); // DI 컨테이너 생성

        services.AddScoped<MaterialDialogService>();
        services.AddScoped<IMessageBoxService>(sp => sp.GetRequiredService<MaterialDialogService>());
        services.AddScoped<IDialogService>(sp => sp.GetRequiredService<MaterialDialogService>());
        services.AddScoped<IWindowService, WindowService>();
        services.AddScoped<INetworkSessionFactory, NetworkSessionFactory>();
        services.AddScoped<ISoundService, SoundService>();
        services.AddScoped<ISnackbarService, SnackbarService>();
        services.AddScoped<IDispatcher, WpfDispatcher>();
        services.AddScoped<IGameSessionService, GameSessionService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddScoped<IDatabaseService, DatabaseService>();
        services.AddScoped<IPlayerTrackerService, PlayerTrackerService>();
        services.AddScoped<IServerCommandService, ServerCommandService>();
        services.AddScoped<IServerRequestService, ServerRequestService>();

        services.AddScoped<IGameClientFactory, GameClientFactory>();
        services.AddScoped<IViewModelFactory, ViewModelFactory>();
        services.AddScoped<Func<IGameServer>>(sp => () => sp.GetRequiredService<IGameServer>());

        //services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddScoped<IMessenger, WeakReferenceMessenger>();

        services.AddScoped<IGameDataRouter, GameDataRouter>();

        services.AddScoped<IGameClient, GameClient>();
        services.AddScoped<IGameServer, GameServer>();
        services.AddScoped<SoloGameClient>();

        services.AddScoped<Dummy>();

        var serviceProvider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(serviceProvider);


        List<Dummy> dummies = new();

        async Task StartStressTest(int count)
        {
            var tasks = Enumerable.Range(0, count).Select(async i =>
            {
                var scope = serviceProvider.CreateScope();
                var scopeIoc = scope.ServiceProvider;

                scopeIoc.GetRequiredService<IGameDataRouter>();
                scopeIoc.GetRequiredService<IServerRequestService>();

                var dummy = scopeIoc.GetRequiredService<Dummy>();

                await dummy.ConnectAsync().ConfigureAwait(false);
                return dummy;
            });

            // 접속 시도 동시에 진행
            var results = await Task.WhenAll(tasks);
            dummies.AddRange(results);
            Console.WriteLine($"{count}개 더미 접속 완료");

            await Task.Delay(1000);

            for (int i = 0; i < count; i++)
            {
                await dummies[i].SendChat();
            }
        }

        await StartStressTest(80);
        await Task.Delay(1000000);
    }
}

class Dummy
{
    public IAuthSessionService AuthSessionService { get; set; }
    public IGameSessionService GameSessionService { get; set; }
    public IServerCommandService ServerCommandService { get; set; }
    public IServerRequestService ServerRequestService { get; set; }

    private TaskCompletionSource<bool> tcs = new();

    public Dummy(IAuthSessionService authSessionService, IGameSessionService gameSessionService,
        IServerCommandService serverCommandService, IServerRequestService serverRequestService, IMessenger messenger)
    {
        AuthSessionService = authSessionService;
        GameSessionService = gameSessionService;
        ServerCommandService = serverCommandService;
        ServerRequestService = serverRequestService;
        messenger.Register<ClientActivatedMessage>(this, (r, m) => tcs.TrySetResult(true));
    }

    public async Task ConnectAsync()
    {
        ConnectionOption connectionOption = new("127.0.0.1", 7777, LoginType.Guest, DoubleThreeRuleType.WhiteOnlyAllow,
            ConnectionType.Client, new CancellationToken(), 3);

        await AuthSessionService.StartSessionAsync(connectionOption);
        await AuthSessionService.RequestGuestLoginAsync();

        await tcs.Task;
    }

    public async Task SendChat()
    {
        await GameSessionService.SendChatAsync("Dummy");
    }

}