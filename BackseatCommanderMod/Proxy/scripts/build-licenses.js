const fs = require("node:fs/promises");
const { promisify } = require("node:util");
const licenseChecker = require("license-checker");

if (process.argv.length !== 3) {
  console.error("USAGE: node build-licenses.js <PATH>");
  process.exit(1);
}

const outputPath = process.argv[2];

const capitalize = ([initial, ...str]) => initial.toUpperCase() + str.join("");

/** @param {licenseChecker.ModuleInfos} moduleInfos */
async function getLicenseFileRows(moduleInfos) {
  const fields = [
    "name",
    "version",
    "description",
    "publisher",
    "email",
    "url",
    "repository",
  ];

  const text = [];
  for (const [package, info] of Object.entries(moduleInfos)) {
    const rows = [
      `# ${package}`,
      ...fields
        .map((field) => ({ key: capitalize(field), value: info[field] }))
        .filter(({ value }) => !!value)
        .map(({ key, value }) => [key, value].join(": ")),
      "License: " +
        (typeof info.licenses === "string"
          ? info.licenses
          : info.licenses.join(", ")) +
        "\n",
      await fs.readFile(info.licenseFile, "utf-8"),
    ];

    text.push(rows.join("\n"), "");
  }

  return text.join("\n");
}

async function run() {
  const licenses = await promisify(licenseChecker.init)({
    start: process.cwd(),
    production: true,
  });

  await fs.writeFile(outputPath, await getLicenseFileRows(licenses), "utf-8");
}

run();
