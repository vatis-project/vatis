// <copyright file="MockMetarRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Net;
using System.Threading.Tasks;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather;

/// <summary>
/// Provides a mock implementation of the <see cref="IMetarRepository"/> interface, intended for testing purposes.
/// </summary>
public class MockMetarRepository : IMetarRepository
{
    private readonly IDownloader _downloader;
    private readonly Decoder.MetarDecoder _metarDecoder;
    private readonly string _localMetarServiceUrl = $"http://{IPAddress.Loopback}:5500/metar?id=";

    /// <summary>
    /// Initializes a new instance of the <see cref="MockMetarRepository"/> class.
    /// </summary>
    /// <param name="downloader">The implementation of <see cref="IDownloader"/> used to handle downloading operations.</param>
    public MockMetarRepository(IDownloader downloader)
    {
        _downloader = downloader;
        _metarDecoder = new Decoder.MetarDecoder();
    }

    /// <inheritdoc />
    public async Task<DecodedMetar?> GetMetar(string station, bool monitor = false, bool triggerMessageBus = true)
    {
        var metar = await _downloader.DownloadStringAsync(_localMetarServiceUrl + station);
        if (!string.IsNullOrEmpty(metar))
        {
            var decodedMetar = _metarDecoder.ParseNotStrict(metar);
            EventBus.Instance.Publish(new MetarReceived(decodedMetar));
            return decodedMetar;
        }

        return null;
    }

    /// <inheritdoc />
    public void RemoveMetar(string station)
    {
        // Ignore
    }
}
