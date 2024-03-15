using SixLabors.ImageSharp;
using System.Globalization;
using System.Text;

namespace VttVideoPreviews.Lib;

public class VttVideoPreviews
{
    private readonly string _videoPath;
    
    private readonly int _thumbRateSeconds;
    private readonly int _thumbWidth;
    private readonly string _spriteFileName = "sprite.jpg";
    private readonly string _vttFileName = "thumbs.vtt";
    public string SpriteFilePath => $"\"{OutputPath}/{_spriteFileName}\"";
    public string VttFilePath => $"{OutputPath}/{_vttFileName}";
    private TimeSpan ThumbSpan => TimeSpan.FromSeconds(_thumbRateSeconds);
    public string OutputPath { get; set; }  
    public string ThumbnailPath { get; set; }  
    private string[] ThumbFiles { get; set; } = default!;
    private int ThumbnailHeight { get; set; } = default!;
    private int ThumbnailWidth { get; set; } = default!;
    private int SpriteColumns { get; set; } = 1;
    private int SpriteRows { get; set; } = 1;
    
    private string GetThumbnailPath(int index) => Path.Combine(ThumbnailPath, $"thumb{index:D3}.jpg");
    
    public VttVideoPreviews(string videoPath, int thumbRateSeconds = 1, int thumbWidth = 120, string? outputPath = null)
    {
        _videoPath = videoPath;
        outputPath ??= Path.Combine(Path.GetDirectoryName(videoPath)!, "previews", Path.GetFileNameWithoutExtension(videoPath));
        OutputPath = outputPath;
        if (!Directory.Exists(OutputPath))
        {
            Directory.CreateDirectory(OutputPath);
        }
        ThumbnailPath = Path.Combine(outputPath, "thumbs");
        if (!Directory.Exists(ThumbnailPath))
        {
            Directory.CreateDirectory(ThumbnailPath);
        }
        _thumbRateSeconds = thumbRateSeconds;
        _thumbWidth = thumbWidth;
    }

    public async Task InstallPrerequisitesAsync()
    {
        var ffmpegInstall = new ThirdPartyInstaller("ffmpeg");
        await ffmpegInstall.Install();
        var imageMagickInstall = new ThirdPartyInstaller("ImageMagick");
        await imageMagickInstall.Install();
    }

    public async Task GenerateAsync()
    {
        await InstallPrerequisitesAsync();
        await GenerateThumbnailsAsync();
        await ResizeThumbnailsAsync();
        await GenerateSpriteAsync();
        GenerateVttFile(ThumbnailWidth, ThumbnailHeight, SpriteColumns, SpriteRows, ThumbSpan, _spriteFileName);
    }

    private async Task GenerateThumbnailsAsync()
    {
        var ffmpegArgs = $"-i \"{_videoPath}\" -f image2 -vf fps=1/{_thumbRateSeconds} \"{ThumbnailPath}/thumb%03d.jpg\"";
        await Executor.ExecAsync("ffmpeg", ffmpegArgs);
    }

    private async Task ResizeThumbnailsAsync()
    {
        ThumbFiles = Directory.GetFiles(ThumbnailPath, "thumb*.jpg");
        var mogrifyArgs = $"-resize {_thumbWidth}x \"{string.Join("\" \"", ThumbFiles)}\"";
        await Executor.ExecAsync("mogrify", mogrifyArgs);
    }

    public async Task GenerateSpriteAsync(bool removeThumbnails = true)
    {
        // Determine the number of columns based on the number of thumbnails
        var numThumbs = ThumbFiles.Length;
        var columns = (int)Math.Ceiling(Math.Sqrt(numThumbs));
        var montageArgs = $"-mode Concatenate -tile {columns}x \"{ThumbnailPath}/thumb*.jpg\" {SpriteFilePath}";
        await Executor.ExecAsync("montage", montageArgs);
        GetThumbnailMeta();
        if (!removeThumbnails) return;
        Directory.Delete(ThumbnailPath, true);
    }

    private void GetThumbnailMeta()
    {
        using Image image = Image.Load(ThumbFiles[0]);
        ThumbnailWidth = image.Width;
        ThumbnailHeight = image.Height;
        SpriteColumns = (int)Math.Ceiling(Math.Sqrt(ThumbFiles.Length));
        SpriteRows = SpriteColumns;
    }
    
    public byte[] GenerateVttFileData(int thumbnailWidth, int thumbnailHeight, int columns, int rows, TimeSpan duration, string spriteFileName)
    {
        var vttBuilder = new StringBuilder();
        vttBuilder.AppendLine("WEBVTT");
        vttBuilder.AppendLine();

        var totalThumbnails = columns * rows;
        var thumbnailDuration = TimeSpan.FromSeconds(duration.TotalSeconds / totalThumbnails);

        for (var i = 0; i < totalThumbnails; i++)
        {
            var column = i % columns;
            var row = i / columns;

            var startTime = FormatTimeSpan(i * thumbnailDuration);
            var endTime = FormatTimeSpan((i + 1) * thumbnailDuration);
            var x = column * thumbnailWidth;
            var y = row * thumbnailHeight;

            vttBuilder.AppendLine($"{startTime} --> {endTime}");
            vttBuilder.AppendLine($"{spriteFileName}#xywh={x},{y},{thumbnailWidth},{thumbnailHeight}");
            vttBuilder.AppendLine();
        }

        // Convert StringBuilder to byte array
        return Encoding.UTF8.GetBytes(vttBuilder.ToString());
    }

    public void GenerateVttFile(int thumbnailWidth, int thumbnailHeight, int columns, int rows, TimeSpan duration,
        string spriteFileName)
    {
        var vttBytes = GenerateVttFileData(thumbnailWidth, thumbnailHeight, columns, rows, duration, spriteFileName);
        File.WriteAllBytes(VttFilePath, vttBytes);
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        return timeSpan.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
    }
    
    public static async Task<(byte[] spriteBytes, byte[] vttBytes)> Generate(string videoPath, int thumbRateSeconds = 1, int thumbWidth = 120, string? outputPath = null, bool cleanUp = true)
    {
        if (cleanUp)
        {
            outputPath = Path.GetTempPath();
        }
        var generator = new VttVideoPreviews(videoPath, thumbRateSeconds, thumbWidth, outputPath);
        await generator.GenerateAsync();
        // Read sprite and VTT into byte arrays
        var spriteBytes = await File.ReadAllBytesAsync(generator.SpriteFilePath);
        var vttBytes = await File.ReadAllBytesAsync(generator.VttFilePath);
        if (cleanUp)
        {
            Directory.Delete(generator.OutputPath);
        }

        return (spriteBytes, vttBytes);
    }
}