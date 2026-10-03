// <copyright file="TransitionLevelFormatterTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Atis.Extensions;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class TransitionLevelFormatterTests
{
    [Theory]
    [InlineData("TRANSITION LEVEL {trl}", 65, "TRANSITION LEVEL 65")]
    [InlineData("TRANSITION LEVEL {trl:###}", 65, "TRANSITION LEVEL 065")]
    [InlineData("TL {trl:#}", 65, "TL 65")]
    [InlineData("TL {trl:####}", 5, "TL 0005")]
    [InlineData("TL {trl:###} {trl}", 65, "TL 065 65")]
    public void Format_NumericPlaceholder(string template, int altitude, string expected)
    {
        Assert.Equal(expected, TransitionLevelFormatter.Format(template, altitude));
    }

    [Fact]
    public void Format_TextPlaceholder_Unchanged()
    {
        Assert.Equal("TL six five", TransitionLevelFormatter.Format("TL {trl|text}", 65));
    }
}
