// <copyright file="DesktopServiceErrorsTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Tmds.DBus.Protocol;
using Vatsim.Vatis.Utils;
using Xunit;

namespace Vatsim.Vatis.Tests.Utils;

public class DesktopServiceErrorsTests
{
    [Theory]
    [InlineData("org.freedesktop.DBus.Error.ServiceUnknown")]
    [InlineData("org.freedesktop.DBus.Error.NameHasNoOwner")]
    public void AMissingService_IsRecognized_AlsoWhenWrapped(string errorName)
    {
        var error = new DBusException(errorName, "The name is not activatable");

        Assert.True(DesktopServiceErrors.IsMissingDBusService(error));
        Assert.True(DesktopServiceErrors.IsMissingDBusService(new AggregateException(error)));
        Assert.True(DesktopServiceErrors.IsMissingDBusService(
            new AggregateException(new AggregateException(error), new DBusException(errorName, "other"))));
    }

    [Fact]
    public void OtherErrors_AreNotIgnored()
    {
        Assert.False(DesktopServiceErrors.IsMissingDBusService(new InvalidOperationException("boom")));
        Assert.False(DesktopServiceErrors.IsMissingDBusService(
            new DBusException("org.freedesktop.DBus.Error.AccessDenied", "denied")));
        Assert.False(DesktopServiceErrors.IsMissingDBusService(new AggregateException(
            new DBusException("org.freedesktop.DBus.Error.ServiceUnknown", "x"), new InvalidOperationException("boom"))));
    }
}
