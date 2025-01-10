// <copyright file="MockMetarRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Net;
using System.Threading.Tasks;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Weather.Decoder;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather;

/// <summary>
/// Represents a mock implementation of the <see cref="IMetarRepository"/> interface,
/// used for retrieving and decoding METAR data in a testable manner.
/// </summary>
public class MockMetarRepository : IMetarRepository
{
    private readonly IDownloader downloader;
    private readonly string localMetarServiceUrl = $"http://{IPAddress.Loopback.ToString()}:5500/metar?id=";
    private readonly MetarDecoder metarDecoder;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockMetarRepository"/> class.
    /// </summary>
    /// <param name="downloader">The <see cref="IDownloader"/> instance to use for downloading METAR data.</param>
    public MockMetarRepository(IDownloader downloader)
    {
        this.downloader = downloader;
        this.metarDecoder = new MetarDecoder();
    }

    /// <inheritdoc/>
    public async Task<DecodedMetar?> GetMetar(string station, bool monitor = false, bool triggerMessageBus = true)
    {
        var metar = await this.downloader.DownloadStringAsync(this.localMetarServiceUrl + station);
        if (!string.IsNullOrEmpty(metar))
        {
            var decodedMetar = this.metarDecoder.ParseNotStrict(metar);
            MessageBus.Current.SendMessage(new MetarReceived(decodedMetar));
            return decodedMetar;
        }

        return null;
    }

    /// <inheritdoc/>
    public void RemoveMetar(string station)
    {
        // Ignore
    }
}
