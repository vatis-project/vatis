// <copyright file="JwtHelper.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Vatsim.Vatis.Utils;

/// <summary>
/// Provides utility methods for creating and handling JSON Web Tokens (JWTs).
/// </summary>
public static class JwtHelper
{
    /// <summary>
    /// Generates a JSON Web Token (JWT) using the specified private key, issuer, audience, and expiration time.
    /// </summary>
    /// <param name="privateKey">The private RSA key used to sign the token.</param>
    /// <param name="keyId">The name of the security key.</param>
    /// <returns>A signed JWT as a string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any of the required parameters are null.</exception>
    /// <exception cref="CryptographicException">Thrown if the provided private key is invalid or cannot be imported.</exception>
    public static string GenerateJwt(string? privateKey, string keyId)
    {
        if (string.IsNullOrEmpty(privateKey))
            throw new ArgumentNullException(nameof(privateKey));

        var privateKeyBytes = Convert.FromBase64String(privateKey);
        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
        var securityKey = new RsaSecurityKey(rsa) { KeyId = keyId };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "vatis.app",
            Audience = "vatis.app",
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)),
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            }
        });
    }
}
