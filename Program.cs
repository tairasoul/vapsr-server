using VapSRServer;

ServerHandler server = new(7777);

await server.Start();