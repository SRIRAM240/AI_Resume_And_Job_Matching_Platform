using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace ResumeJobMatching.Api.Services.Resume;

public class ResumeTextExtractor
{
    public async Task<string> ExtractTextAsync(
        string filePath,
        string extension,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (extension.Equals(
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                using var document = PdfDocument.Open(filePath);

                var extractedPages = new List<string>();

                foreach (var page in document.GetPages())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var words = page.GetWords(
                        NearestNeighbourWordExtractor.Instance);

                    var pageText = string.Join(
                        " ",
                        words.Select(word => word.Text)
                            .Where(text => !string.IsNullOrWhiteSpace(text)));

                    extractedPages.Add(pageText);
                }

                return string.Join(
                    Environment.NewLine,
                    extractedPages);
            }

            if (extension.Equals(
                    ".docx",
                    StringComparison.OrdinalIgnoreCase))
            {
                using var document =
                    WordprocessingDocument.Open(filePath, false);

                return document.MainDocumentPart?
                    .Document?
                    .Body?
                    .InnerText ?? string.Empty;
            }

            throw new InvalidOperationException(
                "Only PDF and DOCX files are supported.");
        }, cancellationToken);
    }
}