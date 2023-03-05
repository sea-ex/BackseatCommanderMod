using System;
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

        protected override void OnOpen()
        {
            base.OnOpen();
            BackseatCommanderMod.Instance.RegisterCommanderServiceSession(this);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            BackseatCommanderMod.Instance.UnregisterCommanderServiceSession(this);
        }

        public delegate void StartHandler(object sender, EventArgs e);
        public event StartHandler OnStart;

        public delegate void StopHandler(object sender, EventArgs e);
        public event StopHandler OnStop;

        public delegate void GyroscopeDataHandler(object sender, GyroscopeDataEventArgs e);
        public event GyroscopeDataHandler OnGyroscopeData;

        protected override void OnMessage(MessageEventArgs e)
        {
            if (!e.IsBinary || e.RawData == null || e.RawData.Length < 1)
            {
                return;
            }

            if (!Enum.IsDefined(typeof(MessageOpCodes), e.RawData[0]))
            {
                return;
            }

            var opcode = (MessageOpCodes)e.RawData[0];
            Static.Logger?.LogDebug($"Received opcode {opcode}");

            switch (opcode)
            {
                case MessageOpCodes.Start:
                    OnStart?.Invoke(this, EventArgs.Empty);
                    break;
                case MessageOpCodes.Stop:
                    OnStop?.Invoke(this, EventArgs.Empty);
                    break;
                case MessageOpCodes.GyroscopeData:
                    {
                        // opcode is 1 byte
                        int payloadOffset = 1;
                        if (e.RawData.Length < (payloadOffset + 4 * sizeof(float))) break;

                        var quaternion = new double[4];
                        for (int i = 0; i < quaternion.Length; i++)
                        {
                            quaternion[i] = BitConverter.ToSingle(e.RawData, payloadOffset + i * sizeof(float));
                        }

                        OnGyroscopeData?.Invoke(this, new GyroscopeDataEventArgs
                        {
                            Angle = new QuaternionD(quaternion[0], quaternion[1], quaternion[2], quaternion[3]).eulerAngles
                        });
                        break;
                    }
                default:
                    break;
            };
        }
    }

    internal class GyroscopeDataEventArgs : EventArgs
    {
        public Vector3d Angle { get; set; }
    }

    internal enum MessageOpCodes : byte
    {
        RESERVED = 0,
        Start = 1,
        Stop = 2,
        GyroscopeData = 3
    }
}
