using System.Diagnostics;
using System.Runtime.CompilerServices;

var repoRoot = GetRepoRoot();

var positional = new List<string>();
var @namespace = "ims";

for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--namespace" && i + 1 < args.Length)
    {
        @namespace = args[++i];
    }
    else
    {
        positional.Add(args[i]);
    }
}

var privateKeyPath = positional.Count > 0 ? positional[0] : Path.Combine(repoRoot, "AccountsService", "AccountsService.API", "Keys", "accounts-private.pem");
var publicKeyPath = positional.Count > 1 ? positional[1] : Path.Combine(repoRoot, "ApiGateway", "Keys", "accounts-public.pem");

if (!File.Exists(privateKeyPath))
{
    throw new InvalidOperationException($"Private key not found at '{privateKeyPath}'.");
}

if (!File.Exists(publicKeyPath))
{
    throw new InvalidOperationException($"Public key not found at '{publicKeyPath}'.");
}

var manifest = RunProcessCapture("kubectl",
[
    "create", "secret", "generic", "ims-jwt-keys",
    $"--from-file=accounts-private.pem={privateKeyPath}",
    $"--from-file=accounts-public.pem={publicKeyPath}",
    "-n", @namespace,
    "--dry-run=client",
    "-o", "yaml"
]);

RunProcessWithInput("kubectl", ["apply", "-n", @namespace, "-f", "-"], manifest);

static string GetRepoRoot([CallerFilePath] string path = "")
{
    return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, ".."));
}

static string RunProcessCapture(string fileName, IEnumerable<string> arguments)
{
    var startInfo = new ProcessStartInfo(fileName) { RedirectStandardOutput = true };
    foreach (var arg in arguments)
    {
        startInfo.ArgumentList.Add(arg);
    }

    using var process = Process.Start(startInfo)!;
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"'{fileName}' exited with code {process.ExitCode}.");
    }

    return output;
}

static void RunProcessWithInput(string fileName, IEnumerable<string> arguments, string input)
{
    var startInfo = new ProcessStartInfo(fileName) { RedirectStandardInput = true };
    foreach (var arg in arguments)
    {
        startInfo.ArgumentList.Add(arg);
    }

    using var process = Process.Start(startInfo)!;
    process.StandardInput.Write(input);
    process.StandardInput.Close();
    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"'{fileName}' exited with code {process.ExitCode}.");
    }
}