// <copyright file="MetarRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace DevServer.Services;

/// <summary>
/// Provides functionality to retrieve METAR data from the VATSIM METAR endpoint.
/// </summary>
public class MetarRepository : IMetarRepository
{
    private const string VatsimMetarServiceUrl = "https://metar.vatsim.net/";
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetarRepository"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public MetarRepository(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc/>
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
