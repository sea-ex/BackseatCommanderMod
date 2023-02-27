using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace BackseatCommanderMod.Server
{
    internal class CommanderServer : IDisposable
    {
        private HttpServer? httpServer;
        private WebSocketServer? server;
        private bool disposedValue;

        public CommanderServer(IPAddress host, int port)
        {
            httpServer = new HttpServer(host, port);
            httpServer.Log.Level = WebSocketSharp.LogLevel.Trace;
            httpServer.DocumentRootPath = "/";

        }

        public void Start()
        {
            if (server == null)
            {
                return;
            }

            server.AddWebSocketService<CommanderService>("/commander");
            server.Start();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    server?.Stop();
                }

                server = null;
                disposedValue = true;
            }
        }


        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
