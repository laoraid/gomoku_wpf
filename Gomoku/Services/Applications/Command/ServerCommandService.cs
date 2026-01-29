using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.DTO;
using Gomoku.Models.Messages;
using Gomoku.Models.Network;
using System.Collections.Concurrent;

namespace Gomoku.Services.Applications.Command
{
    public interface ICommandHandler
    {
        string Command { get; }
        string Description { get; }
        string HelpText { get; }

        IEnumerable<string> Aliases => Enumerable.Empty<string>();
        Task<CommandResult> ExecuteAsync(string[] args, IGameClient client);
    }

    public class ChangeNicknameCommandHandler : ICommandHandler, IRecipient<ChangeNicknameResponseData>
    {
        TaskCompletionSource<CommandResult>? _changeNicknameTcs;

        private IMessenger _messenger;
        public string Command => "changename";
        public string Description => "닉네임을 변경합니다.";
        public string HelpText => "사용법: /changename <새닉네임>";

        public IEnumerable<string> Aliases => new[] { "닉네임변경", "닉변" };

        public ChangeNicknameCommandHandler(IMessenger messenger)
        {
            _messenger = messenger;
        }
        public async Task<CommandResult> ExecuteAsync(string[] args, IGameClient client)
        {
            if (client is null)
                return new CommandResult(false, "접속되지 않았습니다.");

            if (_changeNicknameTcs is not null)
                return new CommandResult(false, "이전 닉네임 변경 요청이 처리 중입니다.");

            if (args.Length < 1)
            {
                return new CommandResult(false, $"사용법: /{Command} <새닉네임>");
            }

            string newnickname = args[0].Trim();

            if (newnickname.ToLowerInvariant().StartsWith("guest"))
            {
                return new CommandResult(false, "Guest 로는 변경할 수 없습니다.");
            }

            var packet = new ChangeNicknameRequestData { NewNickname = newnickname };

            _changeNicknameTcs = new TaskCompletionSource<CommandResult>();
            _messenger.Register<ChangeNicknameResponseData>(this);
            await client.SendDataAsync(packet);

            try
            {
                return await _changeNicknameTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                // 대기 5초
            }
            catch (TimeoutException)
            {
                return new CommandResult(false, "닉네임 변경 요청이 시간 초과되었습니다.");
            }
            finally
            {
                _messenger.Unregister<ChangeNicknameResponseData>(this);
                _changeNicknameTcs = null;
            }
        }

        public void Receive(ChangeNicknameResponseData message)
        {
            if (_changeNicknameTcs == null) // 내 요청 아닐 때
                return;

            var result = message.Accepted
                ? new CommandResult(true, "닉네임이 성공적으로 변경되었습니다.")
                : new CommandResult(false, message.Message ?? "닉네임 변경에 실패했습니다.");

            _changeNicknameTcs?.SetResult(result);
        }
    }

    public class ServerCommandService : IServerCommandService,
        IRecipient<ClientActivatedMessage>,
        IRecipient<ClientDeactivatedMessage>
    {
        IGameClient? _client;
        private readonly ConcurrentDictionary<string, ICommandHandler> _commandHandlers = new();

        private readonly string helpText;

        public ServerCommandService(IMessenger messenger)
        {
            messenger.RegisterAll(this);

            var cmds = new List<ICommandHandler>
            {
                new ChangeNicknameCommandHandler(messenger)
            };

            foreach (var cmd in cmds)
            {
                _commandHandlers.TryAdd(cmd.Command, cmd);

                foreach (var alias in cmd.Aliases)
                {
                    _commandHandlers.TryAdd(alias, cmd);
                }
            }

            var helpTexts = _commandHandlers.Values
                .Select(cmd => $"/{cmd.Command}: {cmd.Description}")
                .OrderBy(text => text);
            helpText = string.Join("\n", helpTexts);
        }

        public void Receive(ClientActivatedMessage message)
        {
            _client = message.Client;
        }

        public void Receive(ClientDeactivatedMessage message)
        {
            _client = null;
        }

        public async Task<CommandResult> ExecuteCommandAsync(string text)
        {
            if (_client is null)
                return new CommandResult(false, "접속되지 않았습니다.");

            if (!text.StartsWith('/'))
                return new CommandResult(false, "명령어는 '/'로 시작해야 합니다.");

            var parts = text[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return new CommandResult(false, "명령어가 입력되지 않았습니다.");

            var command = parts[0].ToLowerInvariant();

            if (command == "help" || command == "도움말" || command == "명령어")
            {
                return new CommandResult(false, helpText);
            }

            var args = parts.Skip(1).ToArray();

            if (_commandHandlers.TryGetValue(command, out var handler))
            {
                return await handler.ExecuteAsync(args, _client);
            }
            else
            {
                return new CommandResult(false, $"알 수 없는 명령어입니다: {command}");
            }
        }
    }
}
