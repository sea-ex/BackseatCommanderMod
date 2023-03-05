import { useCallback, useEffect, useState } from "react";

enum OpCode {
  Start = 1,
  Stop = 2,
  GyroscopeData = 3,
}

const makePacket = (opcode: OpCode, payloadLength: number) => {
  const buf = new Uint8Array(1 + payloadLength);
  buf[0] = opcode;
  return buf;
};

const Packets = {
  Start: () => makePacket(OpCode.Start, 0),
  Stop: () => makePacket(OpCode.Stop, 0),
  GyroscopeData: (quaternion: number[]) => {
    if (quaternion.length !== 4) return null;

    const buf = makePacket(OpCode.GyroscopeData, 4 * 4);
    const view = new DataView(buf.buffer);
    for (let i = 0; i < 4; i++) {
      view.setFloat32(1 + i * 4, quaternion[i], true);
    }

    return buf;
  },
};

const waitUntil = (
  fn: () => boolean,
  interval: number
): Promise<void> & { cancel: () => void } => {
  let timeoutHandle: number | undefined = undefined;

  const promise = new Promise((resolve) => {
    timeoutHandle = setInterval(() => {
      if (!fn()) {
        return;
      }

      if (timeoutHandle) {
        clearInterval(timeoutHandle);
        timeoutHandle = undefined;
      }
      resolve();
    }, interval);
  }) as Promise<void> & { cancel: () => void };

  promise.cancel = () => {
    if (timeoutHandle) {
      clearInterval(timeoutHandle);
      timeoutHandle = undefined;
    }
  };

  return promise;
};

const App = () => {
  const [quaternion, setQuaternion] = useState<number[] | undefined>();

  const [sensor, setSensor] = useState<RelativeOrientationSensor>();

  useEffect(() => {
    if (!sensor) return;

    let ws: WebSocket;
    let onQuaternion: () => void;
    let onError: (err: any) => void;
    let waitPromise: Promise<void> & { cancel: () => void };

    async function start() {
      if (!sensor) return;

      ws = new WebSocket(
        (window.location.protocol === "https:" ? "wss://" : "ws://") +
          window.location.host +
          "/ws"
      );

      waitPromise = waitUntil(() => ws.readyState === ws.OPEN, 100);
      await waitPromise;

      onQuaternion = () => {
        // debugger;
        const quaternion = sensor.quaternion;
        setQuaternion(quaternion);

        if (!quaternion) return;

        const payload = Packets.GyroscopeData(quaternion);
        if (!payload) return;

        ws.send(payload);
      };

      onError = (err: any) => {
        console.error(err);
        debugger;
      };

      sensor.addEventListener("reading", onQuaternion);
      sensor.addEventListener("error", onError);

      sensor.start();
      ws.send(Packets.Start());
    }

    start();

    return () => {
      waitPromise?.cancel();
      if (ws?.readyState === WebSocket.OPEN) {
        ws?.send(Packets.Stop());
      }
      ws?.close();
      sensor.removeEventListener("reading", onQuaternion);
      sensor.removeEventListener("error", onError);
      try {
        sensor.stop();
      } catch (e) {
        console.error("Failed to stop sensor", e);
      }
    };
  }, [sensor]);

  const handleClick = useCallback(() => {
    Promise.all([
      navigator.permissions.query({ name: "accelerometer" as any }),
      navigator.permissions.query({ name: "gyroscope" as any }),
    ]).then((results) => {
      if (results.every((result) => result.state === "granted")) {
        const sensor = new RelativeOrientationSensor({
          frequency: 30,
          referenceFrame: "device",
        });
        setSensor(sensor);
      } else {
        console.error("No permissions to use accelerometer and gyroscope.");
        console.error(results);
      }
    });
  }, []);

  return (
    <div>
      <h1>Hello World!</h1>
      <button onClick={handleClick}>Permissions</button>
      <div>{quaternion ? fmtQuaternion(quaternion) : quaternion}</div>
    </div>
  );
};

function fmtQuaternion(quaternion: number[]) {
  return (
    <pre>
      {quaternion
        .map((val, i) => {
          const rounded = val.toFixed(3);
          return (val > 0 ? " " : "") + rounded;
        })
        .join("\n")}
    </pre>
  );
}

export default App;
