using System.Diagnostics;

var images = args.Length > 0
    ? args
    : ["ims-accounts-api:latest", "ims-inventory-api:latest", "ims-api-gateway:latest"];

var nodesOutput = RunProcessCapture("kubectl", ["get", "nodes", "-o", "jsonpath={.items[*].metadata.name}"]);
var nodes = nodesOutput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

foreach (var image in images)
{
    var tarPath = Path.Combine(Path.GetTempPath(), SanitizeForFileName(image) + ".tar");
    RunProcess("docker", ["save", image, "-o", tarPath]);

    foreach (var node in nodes)
    {
        Console.WriteLine($"Loading {image} into {node}...");
        RunProcess("docker", ["exec", node, "mkdir", "-p", "/kind-load"]);
        RunProcess("docker", ["cp", tarPath, $"{node}:/kind-load/image.tar"]);
        RunProcess("docker", ["exec", node, "ctr", "-n", "k8s.io", "images", "import", "/kind-load/image.tar"]);
        RunProcess("docker", ["exec", node, "rm", "/kind-load/image.tar"]);
    }

    File.Delete(tarPath);
}

static string SanitizeForFileName(string image)
{
    return image.Replace(":", "_").Replace("/", "_");
}

static void RunProcess(string fileName, IEnumerable<string> arguments)
{
    var startInfo = new ProcessStartInfo(fileName);
    foreach (var arg in arguments)
    {
        startInfo.ArgumentList.Add(arg);
    }

    using var process = Process.Start(startInfo)!;
    process.WaitForExit();
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
    return output.Trim();
}