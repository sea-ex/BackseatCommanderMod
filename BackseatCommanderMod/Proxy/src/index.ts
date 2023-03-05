import { parseArgs } from "node:util";
import httpProxy from "http-proxy";
import generateCertificate from "./generateCertificate";
import { ServerResponse } from "node:http";

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
      // change the Origin header to match what the WebsocketSharp expects
      changeOrigin: true,
    })
    .on("error", (err, req, res) => {
      console.error(err);

      if (res instanceof ServerResponse) {
        res.writeHead(500, {
          "Content-Type": "text/plain",
        });
        res.end();
        return;
      }
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
