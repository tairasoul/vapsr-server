using VapSRServer;

int port = int.Parse(Environment.GetEnvironmentVariable("VAPSR_SERVER_PORT", EnvironmentVariableTarget.Process) ?? "7777");
bool debug = int.Parse(Environment.GetEnvironmentVariable("VAPSR_DEBUG", EnvironmentVariableTarget.Process) ?? "0") == 1;

Console.WriteLine($"Starting server on port {port}");

if (debug)
	Console.WriteLine("Logging extra debug information.");

ServerHandler server = new(port, debug);

await server.Start();