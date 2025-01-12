// <copyright file="HttpClientExtensions.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Vatsim.Vatis.Io;

/// <summary>
/// Provides extension methods for HttpClient and HttpResponseMessage to enhance functionality.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Validates the status of the <see cref="HttpResponseMessage"/> to ensure it represents a successful HTTP response.
    /// Throws an exception if the status code indicates a failure.
    /// </summary>
    /// <param name="response">The HTTP response message to validate.</param>
    /// <exception cref="Exception">
    /// Thrown when the HTTP response status code is not successful. The exception includes
    /// the status code and any error content from the response.
    /// </exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task ValidateResponseStatus(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(message))
            {
                throw new Exception($"Request Failed ({response.StatusCode}): " + message);
            }

            throw new Exception($"Request Failed ({response.StatusCode})");
        }
    }
}
