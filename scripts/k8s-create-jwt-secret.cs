// Creates or updates the Kubernetes Secret holding the RS256 JWT signing/verification
// keys, directly from the existing local key files - kept out of the Helm chart itself
// so the same key files docker-compose already uses stay the single source of truth.
// Uses "create --dry-run=client -o yaml | apply" so re-running is safe (idempotent),
// unlike a plain "kubectl create secret" which fails once the secret already exists.
using System.Diagnostics;
using System.Runtime.CompilerServices;

var repoRoot = GetRepoRoot();

var privateKeyPath = args.Length > 0 ? args[0] : Path.Combine(repoRoot, "AccountsService", "AccountsService.API", "Keys", "accounts-private.pem");
var publicKeyPath = args.Length > 1 ? args[1] : Path.Combine(repoRoot, "ApiGateway", "Keys", "accounts-public.pem");

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
    "--dry-run=client",
    "-o", "yaml"
]);

RunProcessWithInput("kubectl", ["apply", "-f", "-"], manifest);

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
