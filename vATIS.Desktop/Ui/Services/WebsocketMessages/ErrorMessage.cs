namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

public class ErrorMessage
{
  public string Key { get; } = "Error";
  public ErrorValue? Value { get; set; }
}

public class ErrorValue
{
  public string Message { get; set; } = string.Empty;
}