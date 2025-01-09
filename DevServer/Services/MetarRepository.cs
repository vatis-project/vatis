namespace DevServer.Services;

public class MetarRepository : IMetarRepository
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string VatsimMetarServiceUrl = "https://metar.vatsim.net/";

    public MetarRepository(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> GetVatsimMetar(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length < 4)
        {
            return null;
        }

        var http = _httpClientFactory.CreateClient();
        var response = await http.GetAsync(VatsimMetarServiceUrl + id.ToUpperInvariant());
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        return null;
    }
}
