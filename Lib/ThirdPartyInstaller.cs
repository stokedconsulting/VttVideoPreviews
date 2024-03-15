using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace VttVideoPreviews.Lib;

// Assumes Chocolatey is installed
// Assumes apt-get is available and sudo permission
// Assumes Homebrew is installed
public class ThirdPartyInstaller
{
    private string PackageName { get; init; }
    private string ValidationCommand { get; init; }

    public ThirdPartyInstaller(string packageName, string? validationCommand = null)
    {
        PackageName = packageName;
        ValidationCommand = validationCommand ?? packageName;
    }
    
    protected async Task<bool>  IsInstalled()
    {;
        var (output, errors) = await Executor.CliAsync(ValidationCommand);
        return string.IsNullOrEmpty(errors);
    }

    public async Task Install()
    {
        var alreadyInstalled = await IsInstalled();
        if (alreadyInstalled) return;
        
        string command;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            command = "choco install ffmpeg";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            command = "sudo apt-get update && sudo apt-get install ffmpeg";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            command = "brew install ffmpeg";
        }
        else
        {
            Console.WriteLine("Unsupported OS.");
            return;
        }

        await Executor.ExecAsync(command);
    }
}