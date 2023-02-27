using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using WebSocketSharp.Server;

namespace BackseatCommanderMod.Server
{
    internal class CommanderServer : IDisposable
    {
        private readonly string publicFacingHost;
        private HttpServer httpServer;
        private bool disposedValue;

        public CommanderServer(IPAddress host, int port, string publicFacingHost)
        {
            httpServer = new HttpServer(host, port);
            this.publicFacingHost = string.IsNullOrWhiteSpace(publicFacingHost) ? $"{host}:{port}" : publicFacingHost.Trim();
            InitializeServer(httpServer);
        }

        public void Start()
        {
            if (httpServer == null)
            {
                return;
            }

            httpServer.Start();

            if (httpServer.IsListening)
            {
                Static.Logger?.LogInfo($"[CommanderServer] HTTP server listening at http://{publicFacingHost}/");
                foreach (var path in httpServer.WebSocketServices.Paths)
                {
                    Static.Logger?.LogInfo($"[CommanderServer] - WebSocket service: {path}");
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (httpServer != null)
                    {
                        httpServer.OnGet -= OnServerGet;
                        httpServer.Stop();
                    }
                }

                httpServer = null;
                disposedValue = true;
            }
        }

        private void InitializeServer(HttpServer server)
        {
            server.Log.Level = WebSocketSharp.LogLevel.Trace;
            server.OnGet += OnServerGet;
            server.AddWebSocketService<CommanderService>(
                "/ws",
                s =>
                {
                    s.OriginValidator = headerValue =>
                        !string.IsNullOrEmpty(headerValue)
                        && Uri.TryCreate(headerValue, UriKind.Absolute, out Uri origin)
                        && origin.Host == publicFacingHost;
                }
            );
        }

        private void OnServerGet(object sender, HttpRequestEventArgs e)
        {
            var req = e.Request;
            var res = e.Response;

            if (req.RawUrl != "/")
            {
                res.Redirect($"http://{publicFacingHost}/");
                return;
            }

            byte[] indexContent = global::BackseatCommanderMod.Properties.Resources.WebIndex;
            res.ContentType = "text/html; charset=utf-8";
            res.ContentEncoding = Encoding.UTF8;
            res.ContentLength64 = indexContent.Length;
            res.Close(indexContent, false);
        }
    }
}
