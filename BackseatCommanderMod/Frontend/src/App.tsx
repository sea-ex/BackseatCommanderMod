import { useCallback, useEffect, useState } from "react";

const App = () => {
  const [quaternion, setQuaternion] = useState<number[] | undefined>();

  const [sensor, setSensor] = useState<RelativeOrientationSensor>();

  useEffect(() => {
    if (!sensor) return;

    const onQuaternion = () => {
      // debugger;
      setQuaternion(sensor.quaternion);
    };
    const onError = (err: any) => {
      console.error(err);
      debugger;
    };

    sensor.addEventListener("reading", onQuaternion);
    sensor.addEventListener("error", onError);

    sensor.start();

    return () => {
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
          frequency: 1,
          referenceFrame: "device",
        });
        setSensor(sensor);
      } else {
        console.log("No permissions to use accelerometer and gyroscope.");
        console.log(results);
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
