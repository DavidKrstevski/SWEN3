using System.Diagnostics;

namespace Paperless_OCRWorker.Services;

public static class PdfTextExtractor
{
    public static async Task<bool> ExtractTextAsync(string pdfPath, string outputPath)
    {
        var pdftotext = new ProcessStartInfo
        {
            FileName = "pdftotext",
            Arguments = $"-layout \"{pdfPath}\" \"{outputPath}\"",
            RedirectStandardError = true
        };

        var process = Process.Start(pdftotext);
        if (process != null)
        {
            string err = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (!string.IsNullOrWhiteSpace(err))
                Console.WriteLine($"pdftotext stderr: {err}");
        }

        return File.Exists(outputPath) && new FileInfo(outputPath).Length > 5;
    }

    public static async Task RunTesseractAsync(string pdfPath, string outputPath)
    {
        var tesseract = new ProcessStartInfo
        {
            FileName = "tesseract",
            Arguments = $"\"{pdfPath}\" \"{outputPath.Replace(".txt", "")}\" -l eng",
            RedirectStandardError = true
        };

        var process = Process.Start(tesseract);
        if (process != null)
        {
            string err = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (!string.IsNullOrWhiteSpace(err))
                Console.WriteLine($"Tesseract stderr: {err}");
        }
    }
}
