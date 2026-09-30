using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace CurriculumGenerator.Services;

public interface IPdfExtractionService
{
    string ExtractTextFromPdf(Stream pdfStream);
}

public class PdfExtractionService : IPdfExtractionService
{
    public string ExtractTextFromPdf(Stream pdfStream)
    {
        using var reader = new PdfReader(pdfStream);
        using var doc = new PdfDocument(reader);

        var pageTexts = new List<string>();
        for (int i = 1; i <= doc.GetNumberOfPages(); i++)
        {
            var strategy = new LocationTextExtractionStrategy();
            pageTexts.Add(PdfTextExtractor.GetTextFromPage(doc.GetPage(i), strategy));
        }

        var raw = string.Join("\n", pageTexts);
        return CleanExtractedText(raw);
    }

    /// <summary>
    /// Normalizes whitespace and removes repeated paragraphs/sentences left over by PDF extraction.
    /// </summary>
    public static string CleanExtractedText(string text)
    {
        var normalized = Regex.Replace(text, @"[ \t]+", " ");
        normalized = Regex.Replace(normalized, @"\n+", "\n");

        var paragraphs = normalized.Split(new[] { "\n\n" }, StringSplitOptions.None);
        var dedupedParas = new List<string>();
        string? lastPara = null;
        foreach (var para in paragraphs)
        {
            var trimmed = para.Trim();
            if (trimmed.Length > 0 && trimmed != lastPara)
                dedupedParas.Add(trimmed);
            lastPara = trimmed;
        }

        var joined = string.Join("\n\n", dedupedParas);
        var flat = joined.Replace("\n", " ");
        flat = Regex.Replace(flat, @"\s+", " ");

        var sentences = flat.Split(new[] { ". " }, StringSplitOptions.None);
        var seen = new HashSet<string>();
        var unique = new List<string>();
        foreach (var s in sentences)
        {
            var clean = s.TrimEnd('.').Trim();
            if (seen.Add(clean))
                unique.Add(s);
        }

        return string.Join(". ", unique);
    }
}
