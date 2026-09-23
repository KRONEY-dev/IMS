// Rebuilds and restarts IMS services in Docker Compose - a compact replacement
// for "docker compose up -d --build" so you don't have to remember it every time.
// Run with no args to rebuild everything, or pass a service name (e.g. api-gateway)
// to rebuild just that one.
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
