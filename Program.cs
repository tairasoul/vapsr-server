using VapSRServer;

int port = int.Parse(System.Environment.GetEnvironmentVariable("VAPSR_SERVER_PORT", EnvironmentVariableTarget.Process) ?? "7777");

Console.WriteLine($"Starting server on port {port}");

ServerHandler server = new(port);

await server.Start();