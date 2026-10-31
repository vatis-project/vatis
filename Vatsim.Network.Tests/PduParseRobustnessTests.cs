using System.Reflection;
using Vatsim.Network.PDU;

namespace Vatsim.Network.Tests;

/// <summary>
/// Throws every PDU parser at malformed input. A bad packet from the network must only ever surface as a
/// <see cref="PDUFormatException" />; any other exception escapes FsdSession as an unhandled crash.
/// </summary>
public class PduParseRobustnessTests
{
    private static readonly string[] JunkValues =
    [
        "", "KLM68N", "@", "@KLM68N", "abc", "-1", "1.5", "NaN", "99999999999999999999", "&", ":", "ZZZZ"
    ];

    private static readonly Assembly NetworkAssembly = typeof(PDUBase).Assembly;

    private static IEnumerable<Type> PduTypes() =>
        NetworkAssembly.GetTypes().Where(t => t is { IsAbstract: false } && t.IsSubclassOf(typeof(PDUBase)));

    private static MethodInfo? FindParse(Type type) =>
        type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null,
            [typeof(string[])], null);

    public static TheoryData<string> ParserNames()
    {
        var data = new TheoryData<string>();
        foreach (var type in PduTypes().Where(t => FindParse(t) != null).OrderBy(t => t.Name))
        {
            data.Add(type.Name);
        }

        return data;
    }

    private static Func<string[], object?> GetParser(string name)
    {
        var type = PduTypes().Single(t => t.Name == name);
        var method = FindParse(type)!;
        return fields =>
        {
            try
            {
                return method.Invoke(null, [fields]);
            }
            catch (TargetInvocationException ex)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
                throw;
            }
        };
    }

    private static void AssertOnlyFormatExceptions(string name, Func<string[], object?> parse, string[] fields)
    {
        try
        {
            parse(fields);
        }
        catch (PDUFormatException)
        {
        }
        catch (Exception ex)
        {
            Assert.Fail($"{name}.Parse([{string.Join(", ", fields.Select(f => $"\"{f}\""))}]) threw " +
                        $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    [Fact]
    public void EveryConcretePdu_HasAStringArrayParseMethod()
    {
        // FastPilotPosition needs the packet type so it has its own Parse overload and its own tests.
        var missing = PduTypes()
            .Where(t => t != typeof(PDUFastPilotPosition) && FindParse(t) == null)
            .Select(t => t.Name)
            .ToList();
        Assert.True(missing.Count == 0, $"PDUs without Parse(string[]): {string.Join(", ", missing)}");
    }

    [Theory]
    [MemberData(nameof(ParserNames))]
    public void Parse_AllFieldsJunk_OnlyThrowsPduFormatException(string name)
    {
        var parse = GetParser(name);
        for (var length = 0; length <= 30; length++)
        {
            foreach (var junk in JunkValues)
            {
                AssertOnlyFormatExceptions(name, parse, Enumerable.Repeat(junk, length).ToArray());
            }
        }
    }

    [Theory]
    [MemberData(nameof(ParserNames))]
    public void Parse_OneJunkFieldAmongNumericOnes_OnlyThrowsPduFormatException(string name)
    {
        // Catches parsing that sits outside a try block: put junk in each position in turn.
        var parse = GetParser(name);
        for (var length = 1; length <= 30; length++)
        {
            for (var position = 0; position < length; position++)
            {
                foreach (var junk in JunkValues)
                {
                    var fields = Enumerable.Repeat("0", length).ToArray();
                    fields[position] = junk;
                    AssertOnlyFormatExceptions(name, parse, fields);
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ParserNames))]
    public void Parse_FormatException_CarriesTheRawPacket(string name)
    {
        var parse = GetParser(name);
        var fields = new[] { "A", "B" };
        var ex = Record.Exception(() => parse(fields));
        if (ex is PDUFormatException format)
        {
            Assert.Equal("A:B", format.RawMessage);
        }
    }

    [Fact]
    public void RadioMessage_CallsignWhereFrequencyBelongs_ThrowsPduFormatException()
    {
        // Real crash report: "The input string 'KLM68N' was not in a correct format."
        var ex = Assert.Throws<PDUFormatException>(() => PDURadioMessage.Parse(["ABC123", "KLM68N", "hello"]));
        Assert.IsType<FormatException>(ex.InnerException);
    }

    [Theory]
    [InlineData("@")]
    [InlineData("")]
    public void RadioMessage_EmptyFrequencyToken_DoesNotEscapeAsUnhandledException(string token)
    {
        AssertOnlyFormatExceptions(nameof(PDURadioMessage), GetParser(nameof(PDURadioMessage)),
            ["ABC123", token + "&@12345", "hello"]);
    }
}
