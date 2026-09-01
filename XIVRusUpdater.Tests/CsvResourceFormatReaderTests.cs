using System.Text;
using XIVRusUpdater.Core.Resource;
using XIVRusUpdater.Core.Resource.Readers;
using Xunit;

namespace XIVRusUpdater.Tests;

public sealed class CsvResourceFormatReaderTests
{
    [Fact]
    public void Read_ReturnsOnlyStringColumnsWithNullTerminatedValues()
    {
        using var stream = Csv(
            "Int32,String,Int32,String",
            "10,Hello,42,World");

        var rows = new CsvResourceFormatReader().Read(stream, "test");

        try
        {
            var row = rows[10];
            Assert.Equal("Hello\0", row[0]!.Value);
            Assert.Equal("World\0", row[1]!.Value);
            Assert.Equal(0, row[0]!.AsReadOnlySpan()[^1]);
            Assert.Equal(0, row[1]!.AsReadOnlySpan()[^1]);
        } finally
        {
            FileResource.DisposeRows(rows);
        }
    }

    [Fact]
    public void Read_IgnoresServiceRowsInvalidIdsAndEmptyRows()
    {
        using var stream = Csv(
            "key,ignored",
            "#,ignored",
            "offset,ignored",
            ",ignored",
            "Int32,String",
            "-1,negative",
            "not-an-id,invalid",
            "7,valid");

        var rows = new CsvResourceFormatReader().Read(stream, "test");

        try
        {
            Assert.Single(rows);
            Assert.Equal("valid\0", rows[7][0]!.Value);
        } finally
        {
            FileResource.DisposeRows(rows);
        }
    }

    [Fact]
    public void Read_ThrowsWhenRowHasFewerColumnsThanHeader()
    {
        using var stream = Csv(
            "Int32,String,Int32,String",
            "3,OnlyFirstString");

        Assert.Throws<InvalidDataException>(() => new CsvResourceFormatReader().Read(stream, "test"));
    }

    [Fact]
    public void Read_ParsesLuminaMacroString()
    {
        const string macro =
            "<settime(lnum1)><if([gnum77==3],<num(t_day)>/<num(t_mon)>/<num(t_year)>,<num(t_mon)>/<num(t_day)>/<num(t_year)>)> <num(t_hour)>:<sec(t_min)>";
        using var stream = Csv(
            "Int32,String",
            $"42,\"{macro}\"");

        var rows = new CsvResourceFormatReader().Read(stream, "test");

        try
        {
            var value = rows[42][0];

            Assert.NotNull(value);
            Assert.False(value!.IsError);
            Assert.NotEmpty(value.AsReadOnlySpan().ToArray());
            Assert.Equal(0, value.AsReadOnlySpan()[^1]);
        } finally
        {
            FileResource.DisposeRows(rows);
        }
    }

    [Fact]
    public void Read_ReturnsErrorForInvalidLuminaMacroString()
    {
        const string invalidMacro = "<if([gnum77==3],<num(t_day)>";
        using var stream = Csv(
            "Int32,String",
            $"42,\"{invalidMacro}\"");

        var rows = new CsvResourceFormatReader().Read(stream, "test");

        try
        {
            var value = rows[42][0];

            Assert.NotNull(value);
            Assert.True(value!.IsError);
            Assert.StartsWith("Row 42, column 1:", value.Error);
        }
        finally
        {
            FileResource.DisposeRows(rows);
        }
    }

    [Fact]
    public void Read_ReturnsErrorForDoubleEscapedMacroComma()
    {
        const string invalidMacro = "<if([lnum1==0],text\\\\,text,)>";
        using var stream = Csv(
            "Int32,String",
            $"42,\"{invalidMacro}\"");

        var rows = new CsvResourceFormatReader().Read(stream, "test");

        try
        {
            var value = rows[42][0];

            Assert.NotNull(value);
            Assert.True(value!.IsError);
            Assert.Contains("Double-escaped comma", value.Error);
        }
        finally
        {
            FileResource.DisposeRows(rows);
        }
    }

    [Fact]
    public void Read_KeepsTheFirstOccurrenceOfDuplicateRowId()
    {
        using var stream = Csv(
            "Int32,String",
            "12,first",
            "12,second");

        var rows = new CsvResourceFormatReader().Read(stream, "test");

        try
        {
            Assert.Single(rows);
            Assert.Equal("first\0", rows[12][0]!.Value);
        } finally
        {
            FileResource.DisposeRows(rows);
        }
    }

    [Fact]
    public void Read_ParsesCsvFixtureFile()
    {
        using var stream = File.OpenRead(GetFixturePath("sample.csv"));
        var rows = new CsvResourceFormatReader().Read(stream, "sample");

        try
        {
            Assert.Equal(19_592, rows.Count);
            Assert.Equal("\0", rows[0][0]!.Value);
            Assert.Equal("TestValue_1\0", rows[1][0]!.Value);
            Assert.Equal("TestValue_6\0", rows[6][0]!.Value);
            Assert.Equal("\0", rows[102_709][0]!.Value);

            Assert.All(rows, entry =>
            {
                Assert.Single(entry.Value);

                var expected = entry.Value[0]!.Value == "\0"
                                   ? "\0"
                                   : $"TestValue_{entry.Key}\0";

                Assert.Equal(expected, entry.Value[0]!.Value);
            });
        } finally
        {
            FileResource.DisposeRows(rows);
        }
    }

    private static MemoryStream Csv(params string[] lines) =>
        new(Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines)));

    private static string GetFixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
}
