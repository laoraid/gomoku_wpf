using Gomoku.Models.Common;
using Gomoku.Models.Network;
using Microsoft.Extensions.DependencyInjection;

namespace Gomoku.Models
{
    public interface IGameClientFactory
    {
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
