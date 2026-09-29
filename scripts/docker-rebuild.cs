using System.Diagnostics;
using System.Runtime.CompilerServices;

var repoRoot = GetRepoRoot();
var service = args.Length > 0 ? args[0] : null;

var upArgs = new List<string> { "compose", "up", "-d", "--build" };
if (!string.IsNullOrEmpty(service))
{
    upArgs.Add(service);
}

RunProcess("docker", upArgs, repoRoot);
RunProcess("docker", ["compose", "ps"], repoRoot);

static string GetRepoRoot([CallerFilePath] string path = "")
{
    return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, ".."));
}

static int RunProcess(string fileName, IEnumerable<string> arguments, string workingDirectory)
{
    var startInfo = new ProcessStartInfo(fileName) { WorkingDirectory = workingDirectory };
    foreach (var arg in arguments)
    {
        startInfo.ArgumentList.Add(arg);
    }

    using var process = Process.Start(startInfo)!;
    process.WaitForExit();
    return process.ExitCode;
}