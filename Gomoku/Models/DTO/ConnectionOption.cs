namespace Gomoku.Models.DTO
{
    public enum ConnectionType
    {
        Server, Client, Single
    }

    public record ConnectionOption(string Ip, int port, bool IsAuthMode,
        DoubleThreeRuleType DoubleThreeRuleType,
        ConnectionType ConnectionType, CancellationToken CancellationToken, int LeftCancelCount);
}
