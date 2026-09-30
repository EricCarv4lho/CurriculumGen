using Xunit;

using CurriculumGenerator.Services;

namespace CurriculumGenerator.Tests;

public class PdfExtractionServiceTests
{
    [Fact]
    public void CollapsesRepeatedWhitespace()
    {
        var result = PdfExtractionService.CleanExtractedText("Hello   world\n\n\nnext   line");
        Assert.DoesNotContain("   ", result);
    }

    [Fact]
    public void RemovesRepeatedSentences()
    {
        var result = PdfExtractionService.CleanExtractedText("I led a team. I shipped a product. I led a team.");
        Assert.Equal("I led a team. I shipped a product", result);
    }

    [Fact]
    public void KeepsDistinctSentences()
    {
        var result = PdfExtractionService.CleanExtractedText("I led a team. I shipped a product.");
        Assert.Contains("I led a team", result);
        Assert.Contains("I shipped a product.", result);
    }
}
