
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

var filePath = "/Users/avinashkanaujia/Downloads/AVINASH_RESUME.pdf";

using var document = PdfDocument.Open(filePath);

foreach (var page in document.GetPages())
{
    Console.WriteLine($"--- PAGE {page.Number} ---");

    var words = page.GetWords().ToList();

    var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);

    foreach (var block in blocks)
    {
        Console.WriteLine(block.Text);
    }
}
