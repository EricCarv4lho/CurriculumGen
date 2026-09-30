using Xunit;

using CurriculumGenerator.Services;

namespace CurriculumGenerator.Tests;

public class SectionHeaderDetectionTests
{
    [Theory]
    [InlineData("Experiência", "pt", true)]
    [InlineData("Habilidades", "pt", true)]
    [InlineData("Experience", "en", true)]
    [InlineData("Skills", "en", true)]
    [InlineData("Berufserfahrung", "de", true)]
    [InlineData("Maria da Silva", "en", false)]
    [InlineData("My duties included leading a team of engineers", "en", false)]
    public void DetectsSectionHeaders(string text, string lang, bool expected)
    {
        Assert.Equal(expected, CurriculumService.IsSectionHeader(text, lang));
    }
}
