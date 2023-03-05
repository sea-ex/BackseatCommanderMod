import path from "node:path";
import fs from "node:fs";
import { promisify } from "node:util";
import forge from "node-forge";

export default async function generateCertificate(): Promise<{
  cert: string;
  key: string;
}> {
  //! generateCertificate is based on Parcel (https://github.com/parcel-bundler/parcel/blob/19fe7ff00f28f44300fe803c4e594b9fc02b25ad/packages/core/utils/src/generateCertificate.js#L7)
  //! Licensed under the MIT license:
  //!
  //! MIT License
  //!
  //! Copyright (c) 2017-present Devon Govett
  //!
  //! Permission is hereby granted, free of charge, to any person obtaining a copy
  //! of this software and associated documentation files (the "Software"), to deal
  //! in the Software without restriction, including without limitation the rights
  //! to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  //! copies of the Software, and to permit persons to whom the Software is
  //! furnished to do so, subject to the following conditions:
  //!
  //! The above copyright notice and this permission notice shall be included in all
  //! copies or substantial portions of the Software.
  //!
  //! THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  //! IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  //! FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  //! AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  //! LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  //! OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  //! SOFTWARE.
  // const certDirectory = path.dirname(process.execPath);

  // const privateKeyPath = path.join(certDirectory, "self-signed.pem");
  // const certPath = path.join(certDirectory, "self-signed.crt");

  const pki = forge.pki;
  const keys = await promisify(pki.rsa.generateKeyPair)({
    bits: 2048,
    workers: -1,
  });
  const cert = pki.createCertificate();

  cert.publicKey = keys.publicKey;
  cert.serialNumber = Date.now().toString();
  cert.validity.notBefore = new Date();
  cert.validity.notAfter = new Date();
  cert.validity.notAfter.setFullYear(cert.validity.notBefore.getFullYear() + 1);

  const attrs = [
    {
      name: "commonName",
      value: "self-signed.example.com",
    },
    {
      name: "countryName",
      value: "US",
    },
    {
      shortName: "ST",
      value: "Virginia",
    },
    {
      name: "localityName",
      value: "Blacksburg",
    },
    {
      name: "organizationName",
      value: "KSP2 Backseat Commander Mod",
    },
    {
      shortName: "OU",
      value: "Test",
    },
  ];

  const altNames = [
    { type: 2, value: "localhost" },
    { type: 7, ip: "127.0.0.1" },
  ];

  cert.setSubject(attrs);
  cert.setIssuer(attrs);
  cert.setExtensions([
    { name: "basicConstraints", critical: true, cA: false },
    {
      name: "keyUsage",
      critical: true,
      keyCertSign: false,
      digitalSignature: true,
      nonRepudiation: false,
      keyEncipherment: false,
      dataEncipherment: false,
    },
    {
      name: "extKeyUsage",
      critical: true,
      serverAuth: true,
      clientAuth: false,
      codeSigning: false,
      emailProtection: false,
      timeStamping: false,
    },
    {
      name: "subjectAltName",
      altNames,
    },
    {
      name: "subjectKeyIdentifier",
    },
  ]);

  cert.sign(keys.privateKey, forge.md.sha256.create());

  const privPem = pki.privateKeyToPem(keys.privateKey);
  const certPem = pki.certificateToPem(cert);

  // await fs.promises.mkdir(certDirectory, { recursive: true });
  // await fs.promises.writeFile(privateKeyPath, privPem, "utf-8");
  // await fs.promises.writeFile(certPath, certPem, "utf-8");

  return {
    key: privPem,
    cert: certPem,
  };
}
