import { parseArgs } from "node:util";
import httpProxy from "http-proxy";
import generateCertificate from "./generateCertificate";

async function start({
  bindAddress = "0.0.0.0",
  bindPort = "6673",
  target = "http://127.0.0.1:6674",
}: {
  bindAddress?: string;
  bindPort?: string;
  target?: string;
}): Promise<void> {
  console.log("Generating a self-signed TLS certificate...");
  const { cert, key } = await generateCertificate();
  const port = parseInt(bindPort, 10);

  console.log("Starting...");
  httpProxy
    .createProxyServer({
      target,
      localAddress: bindAddress,
      ws: true,
      ssl: { key, cert },
    })
    .once("error", (err) => {
      console.error(err);
      process.exit(1);
    })
    .listen(port);

  console.log(
    `Listening on https://${bindAddress}:${port} and proxying to ${target}`
  );
}

const {
  values: { bindAddress, bindPort, target },
} = parseArgs({
  options: {
    target: {
      type: "string",
      short: "t",
      default: "http://127.0.0.1:6674",
    },
    bindAddress: {
      type: "string",
      short: "h",
      default: "0.0.0.0",
    },
    bindPort: {
      type: "string",
      short: "p",
      default: "6673",
    },
  },
});

start({
  bindAddress,
  bindPort,
  target,
});
