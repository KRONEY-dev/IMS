// Generates a new EF Core migration for one of the IMS microservices - a compact
// replacement for manually typing "dotnet ef migrations add" with the correct
// --project/--startup-project/--output-dir paths, which are easy to mix up.
// Run with no args for an interactive prompt, or pass "accounts"/"inventory" and
// a migration name directly (e.g. "accounts AddSomething").
using System.Diagnostics;
using System.Runtime.CompilerServices;

var repoRoot = GetRepoRoot();

var service = args.Length > 0 ? args[0] : null;
var name = args.Length > 1 ? args[1] : null;

if (string.IsNullOrEmpty(service))
{
    Console.WriteLine("Select a service:");
    Console.WriteLine("  1) accounts");
    Console.WriteLine("  2) inventory");
    Console.Write("Enter 1 or 2: ");
    var choice = Console.ReadLine();

    service = choice switch
    {
        "1" => "accounts",
        "2" => "inventory",
        _ => throw new InvalidOperationException($"Invalid choice '{choice}' - expected 1 or 2.")
    };
}
else if (service is not ("accounts" or "inventory"))
{
    throw new InvalidOperationException($"Invalid service '{service}' - expected 'accounts' or 'inventory'.");
}

if (string.IsNullOrEmpty(name))
{
    Console.Write("Migration name: ");
    name = Console.ReadLine();
}

if (string.IsNullOrEmpty(name))
{
    throw new InvalidOperationException("Migration name cannot be empty.");
}

var (project, startupProject) = service switch
{
    "accounts" => ("AccountsService/AccountsService.Infrastructure/AccountsService.Infrastructure.csproj",
                    "AccountsService/AccountsService.API/AccountsService.API.csproj"),
    "inventory" => ("InventoryService/InventoryService.Infrastructure/InventoryService.Infrastructure.csproj",
                     "InventoryService/InventoryService.API/InventoryService.API.csproj"),
    _ => throw new InvalidOperationException($"Unknown service '{service}'.")
};

RunProcess("dotnet", ["tool", "restore"], repoRoot);

var hasPendingChangesExitCode = RunProcess("dotnet",
    ["ef", "migrations", "has-pending-model-changes", "--project", project, "--startup-project", startupProject],
    repoRoot);

if (hasPendingChangesExitCode == 0)
{
    Console.WriteLine();
    Console.WriteLine("No model changes detected since the last migration - skipping, a new migration would be empty.");
    return;
}

var addExitCode = RunProcess("dotnet",
    ["ef", "migrations", "add", name, "--project", project, "--startup-project", startupProject, "--output-dir", "Database/Migrations"],
    repoRoot);

if (addExitCode == 0)
{
    Console.WriteLine();
    Console.WriteLine("Migration generated. Before committing, open the new file under Database/Migrations and review Up()/Down() by hand - EF generation is not guaranteed to be correct (e.g. a rename can come out as drop+add).");
}

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
