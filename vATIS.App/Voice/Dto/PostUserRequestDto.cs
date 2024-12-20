namespace Vatsim.Vatis.Voice.Dto;
public class PostUserRequestDto
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Client { get; set; }

    public PostUserRequestDto(string username, string password, string client)
    {
        Username = username;
        Password = password;
        Client = client;
    }
}
