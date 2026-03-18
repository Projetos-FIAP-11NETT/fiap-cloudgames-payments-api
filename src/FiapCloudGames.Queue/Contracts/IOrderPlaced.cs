namespace FiapCloudGames.Queue.Contracts;

public interface IOrderPlaced
{
    public int OrderId { get; }
    public Guid UserId { get; }
    public Guid GameId { get; }
    public decimal Price { get; }
    public string Email { get; }
    public string Name { get; }
}

