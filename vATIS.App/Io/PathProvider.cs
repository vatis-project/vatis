using System.IO;

namespace Vatsim.Vatis.Io;

public static class PathProvider
{
    private static string _appDataPath = "";
    public static string LogsFolderPath => Path.Combine(_appDataPath, "Logs");
    public static string ProfilesFolderPath => Path.Combine(_appDataPath, "Profiles");
    public static string AppConfigFilePath => Path.Combine(_appDataPath, "AppConfig.json");
    public static string AirportsFilePath => Path.Combine(_appDataPath, "Airports.json");
    public static string NavaidsFilePath => Path.Combine(_appDataPath, "Navaids.json");
    public static string NavDataSerialFilePath => Path.Combine(_appDataPath, "NavDataSerial.json");
    public static string GetProfilePath(string profileId) => Path.Combine(ProfilesFolderPath, profileId + ".json");
    public static void SetAppDataPath(string path)
    {
        _appDataPath = path;
    }
}