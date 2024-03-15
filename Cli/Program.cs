if (args.Length < 1)
{
    Console.WriteLine("Usage: VttVideoPreviews <videoPath> [outputPath] [thumbRateSeconds] [thumbWidth]");
    return;
}

var videoPath = args[0]
    .Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
    .Replace("//", "/");

if (!File.Exists(videoPath))
{
    Console.WriteLine($"Video file {videoPath} does not exist.");
    return;
}
string? outputPath = args.Length > 1 ? args[1] : null;
int thumbRateSeconds = args.Length > 2 ? int.Parse(args[2]) : 1;
int thumbWidth = args.Length > 3 ? int.Parse(args[3]) : 120;

var generator = new VttVideoPreviews.Lib.VttVideoPreviews(videoPath, thumbRateSeconds, thumbWidth);
await generator.GenerateAsync();

Console.WriteLine($"Video Previews files sprite.jpg and thumbs.vtt have been created here {generator.OutputPath}.");