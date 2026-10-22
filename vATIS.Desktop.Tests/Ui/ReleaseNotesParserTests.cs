using System.Linq;
using Vatsim.Vatis.Ui.ViewModels;
using Xunit;

namespace Vatsim.Vatis.Tests.Ui;

public class ReleaseNotesParserTests
{
    [Fact]
    public void Parse_SplitsVersionSectionsAndItems()
    {
        var parsed = ReleaseNotesParser.Parse("## v4.1.0-beta.20\n### Added\n* One `a`\n* Two\n### Fixed\n* Three\n");

        Assert.Equal("4.1.0-beta.20", parsed.Version);
        Assert.Equal(["Added", "Fixed"], parsed.Sections.Select(s => s.Title));
        Assert.Equal(["One `a`", "Two"], parsed.Sections[0].Items);
        Assert.Equal(["Three"], parsed.Sections[1].Items);
    }

    [Fact]
    public void Parse_StripsLinksAndBold()
    {
        var parsed = ReleaseNotesParser.Parse("### Changed\n* **Bold** and [a link](https://example.com)\n");

        Assert.Equal(["Bold and a link"], parsed.Sections[0].Items);
    }

    [Fact]
    public void Parse_ItemsWithoutSectionGoToNotes()
    {
        var parsed = ReleaseNotesParser.Parse("- Just a line\n");

        Assert.Null(parsed.Version);
        Assert.Equal("Notes", parsed.Sections.Single().Title);
    }

    [Fact]
    public void Parse_EmptyOrNullHasNoSections()
    {
        Assert.Empty(ReleaseNotesParser.Parse(null).Sections);
        Assert.Empty(ReleaseNotesParser.Parse("### Added\n").Sections);
    }
}
