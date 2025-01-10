namespace DevServer.Hub.Dto;

public class ServerDto
{
    public string? ConnectionId { get; set; }
    public AtisHubDto? Dto { get; set; }
    public DateTime UpdatedAt { get; set; }
}
