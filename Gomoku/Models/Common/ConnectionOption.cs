using Gomoku.Models.Domain;

namespace Gomoku.Models.Common
{
    public enum ConnectionType
    {
        Server, Client, Single
    }

    public record ConnectionOption(string Ip, int port, LoginType LoginType,
        DoubleThreeRuleType DoubleThreeRuleType,
        ConnectionType ConnectionType, CancellationToken CancellationToken, int LeftCancelCount);
}
