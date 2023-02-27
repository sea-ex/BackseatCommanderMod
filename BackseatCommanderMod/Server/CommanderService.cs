using System;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace BackseatCommanderMod.Server
{
    internal class CommanderService : WebSocketBehavior
    {
        public void OnTimeRateIndexChanged(int index)
        {
            this.Sessions.Broadcast("Time rate: " + index.ToString());
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            string type = "unknown";
            if (e.IsBinary)
            {
                type = "binary";
            }
            else if (e.IsText)
            {
                type = "text";
            }
            else if (e.IsPing)
            {
                type = "ping";
            }

            Static.Logger?.LogInfo($"Received message. Type: {type}, Data: {e.Data}");
        }
    }
}
