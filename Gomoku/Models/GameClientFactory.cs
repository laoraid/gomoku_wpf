using Gomoku.Models.Common;
using Gomoku.Models.Network;
using Microsoft.Extensions.DependencyInjection;

namespace Gomoku.Models
{
    public interface IGameClientFactory
    {
        /// <summary>
        /// ConnectionType을 보고 클라이언트 객체를 생성합니다.
        /// </summary>
        /// <param name="type">생성할 클라이언트 객체 타입</param>
        /// <returns></returns>
        IGameClient CreateClient(ConnectionType type);
    }

    public class GameClientFactory(IServiceProvider provider) : IGameClientFactory
    {
        public IGameClient CreateClient(ConnectionType type)
        {
            return type switch
            {
                ConnectionType.Single => provider.GetRequiredService<SoloGameClient>(),
                ConnectionType.Client or ConnectionType.Server => provider.GetRequiredService<IGameClient>(),
                _ => throw new NotImplementedException()
            };
        }
    }
}
