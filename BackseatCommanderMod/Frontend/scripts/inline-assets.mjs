import fs from "node:fs/promises";
import { promisify } from "node:util";
import inliner from "web-resource-inliner";

const BUILD_HTML_PATH = "./dist/index.html";

const normalizeLineEndings = (file) => file.replace(/\r\n/g, "\n");

const content = await fs.readFile(BUILD_HTML_PATH, "utf-8");

const inlinedHtml = await promisify(inliner.html)({
  fileContent: normalizeLineEndings(content),
  relativeTo: "./dist",
});

await fs.writeFile(BUILD_HTML_PATH, inlinedHtml, "utf-8");
