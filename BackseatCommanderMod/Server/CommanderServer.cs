using System;
using System.Net;
using System.Text;
using WebSocketSharp.Server;

namespace BackseatCommanderMod.Server
{
    internal class CommanderServer : IDisposable
    {
        //private readonly IServerCertificateGenerator certificateGenerator;
        private readonly string publicUrl;
        private HttpServer httpServer;
        private bool disposedValue;

        public CommanderService CommaderService { get; private set; }

        public CommanderServer(
            IPAddress host,
            int port,
            string publicFacingUrl
        )
        {
            this.httpServer = new HttpServer(host, port);
            this.publicUrl = string.IsNullOrWhiteSpace(publicFacingUrl) ? $"http://{host}:{port}" : publicFacingUrl.Trim();
        }

        public void Start()
        {
            if (httpServer == null)
            {
                return;
            }

            InitializeServer(httpServer);

            httpServer.Start();

            if (httpServer.IsListening)
            {
                Static.Logger?.LogInfo($"[CommanderServer] HTTP server listening at {publicUrl}");
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

                    CommaderService = null;
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
                    //s.OriginValidator = headerValue =>
                    //    !string.IsNullOrEmpty(headerValue)
                    //    && Uri.TryCreate(headerValue, UriKind.Absolute, out Uri origin)
                    //    && origin.Host == publicFacingHost;

                    CommaderService = s;
                }
            );
        }

        private void OnServerGet(object sender, HttpRequestEventArgs e)
        {
            var req = e.Request;
            var res = e.Response;

            if (req.RawUrl != "/")
            {
                res.Redirect($"{publicUrl}/");
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
