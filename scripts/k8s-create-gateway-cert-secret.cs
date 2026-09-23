// Creates or updates the Kubernetes Secret holding the Gateway's HTTPS certificate,
// directly from the existing local gateway.pfx file - kept out of the Helm chart so
// the same file docker-compose already uses stays the single source of truth.
//
// Mounting it as a Secret (rather than relying on the copy baked into the image via
// the Dockerfile) sidesteps a real, Linux-specific failure: "dotnet dev-certs https"
// exports the .pfx with restrictive file permissions, which don't survive being
// copied into a final image that runs as a different, non-root user - the container
// then gets "Access to the path denied" on startup. A Kubernetes Secret volume is
// mounted with its own, always-readable permissions, regardless of the file's
// original permissions on disk.
//
// Uses "create --dry-run=client -o yaml | apply" so re-running is safe (idempotent),
// unlike a plain "kubectl create secret" which fails once the secret already exists.
using System.Diagnostics;
using System.Runtime.CompilerServices;

var repoRoot = GetRepoRoot();

var certPath = args.Length > 0 ? args[0] : Path.Combine(repoRoot, "ApiGateway", "Certs", "gateway.pfx");

if (!File.Exists(certPath))
{
    throw new InvalidOperationException($"Gateway certificate not found at '{certPath}'.");
}

var manifest = RunProcessCapture("kubectl",
[
    "create", "secret", "generic", "ims-gateway-cert",
    $"--from-file=gateway.pfx={certPath}",
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
