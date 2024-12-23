using System.Text.Json.Nodes;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

public class MessageBase
{
  public string Key { get; set; } = string.Empty;

  public JsonValue? Value { get; set; }
}