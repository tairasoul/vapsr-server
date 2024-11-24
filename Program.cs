using VapSRServer;

int port = int.Parse(System.Environment.GetEnvironmentVariable("VAPSR_SERVER_PORT", EnvironmentVariableTarget.Process) ?? "7777");

ServerHandler server = new(port);

await server.Start();