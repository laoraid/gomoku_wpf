namespace Gomoku.Models.DTO
{
    public enum ConnectionType
    {
        Server, Client, Single
    }

    public record ConnectionOption(string Ip, int port, string nickname,
        DoubleThreeRuleType DoubleThreeRuleType,
        ConnectionType ConnectionType, CancellationToken CancellationToken);
}
