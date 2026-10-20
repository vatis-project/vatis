// <copyright file="DesktopServiceErrors.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using Tmds.DBus.Protocol;

namespace Vatsim.Vatis.Utils;

/// <summary>
/// Recognizes errors from desktop services that are not running, which are harmless and not worth showing to the user.
/// </summary>
public static class DesktopServiceErrors
{
    private const string ServiceUnknown = "org.freedesktop.DBus.Error.ServiceUnknown";
    private const string NameHasNoOwner = "org.freedesktop.DBus.Error.NameHasNoOwner";

    /// <summary>
    /// Determines whether the exception only says that a D-Bus service on the desktop is not running. Avalonia talks
    /// to desktop services on Linux, such as the global application menu or the input method, and reports a failure
    /// from a service that is missing as an unobserved task exception.
    /// </summary>
    /// <param name="exception">The exception, which may wrap others.</param>
    /// <returns>True if every exception in it is a missing D-Bus service.</returns>
    public static bool IsMissingDBusService(Exception exception)
    {
        return exception is AggregateException aggregate
            ? aggregate.Flatten().InnerExceptions.All(IsMissingDBusService)
            : exception is DBusException { ErrorName: ServiceUnknown or NameHasNoOwner };
    }
}
