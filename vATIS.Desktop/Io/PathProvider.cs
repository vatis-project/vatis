using System.IO;

namespace Vatsim.Vatis.Io;

public static class PathProvider
{
    private static string s_appDataPath = "";
    public static string LogsFolderPath => Path.Combine(s_appDataPath, "Logs");
    public static string ProfilesFolderPath => Path.Combine(s_appDataPath, "Profiles");
    public static string AppConfigFilePath => Path.Combine(s_appDataPath, "AppConfig.json");
    public static string AirportsFilePath => Path.Combine(s_appDataPath, "Airports.json");
    public static string NavaidsFilePath => Path.Combine(s_appDataPath, "Navaids.json");
    public static string NavDataSerialFilePath => Path.Combine(s_appDataPath, "NavDataSerial.json");
    public static string GetProfilePath(string profileId) => Path.Combine(ProfilesFolderPath, profileId + ".json");
    public static void SetAppDataPath(string path)
    {
        s_appDataPath = path;
    }
}
