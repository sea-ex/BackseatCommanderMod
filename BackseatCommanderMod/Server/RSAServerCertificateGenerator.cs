using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Security.Cryptography.X509Certificates;

namespace BackseatCommanderMod.Server
{
    internal class RSAServerCertificateGenerator : IServerCertificateGenerator
    {
        private readonly string signatureAlgo;
        private readonly int rsaKeyLength;

        public RSAServerCertificateGenerator(string signatureAlgo, int rsaKeyLength)
        {
            this.signatureAlgo = signatureAlgo;
            this.rsaKeyLength = rsaKeyLength;
        }

        public X509Certificate2 GenerateNew()
        {
            var keypair = GenerateNewKeypair(rsaKeyLength);

            var certGenerator = new X509V3CertificateGenerator();
            var cn = new X509Name("CN=self-signed.backseat-commander.example.com");

            certGenerator.SetSerialNumber(new BigInteger(DateTime.UtcNow.Ticks.ToString()));
            certGenerator.SetSubjectDN(cn);
            certGenerator.SetIssuerDN(cn);
            certGenerator.SetNotAfter(DateTime.Now.AddDays(1));
            certGenerator.SetNotBefore(DateTime.Now.Subtract(new TimeSpan(0, 0, 1, 0)));
            // Mark usage for TLS server authentication
            // http://oid-info.com/get/1.3.6.1.5.5.7.3.1
            certGenerator.AddExtension(X509Extensions.ExtendedKeyUsage.Id, true, new ExtendedKeyUsage(KeyPurposeID.id_kp_serverAuth));
            certGenerator.SetPublicKey(keypair.Public);

            var cert = certGenerator.Generate(new Asn1SignatureFactory(signatureAlgo, keypair.Private));
            return new X509Certificate2(DotNetUtilities.ToX509Certificate(cert));
        }

        private AsymmetricCipherKeyPair GenerateNewKeypair(int rsaLength)
        {
            var keypairGenerator = new RsaKeyPairGenerator();
            keypairGenerator.Init(
                new KeyGenerationParameters(
                    new SecureRandom(new CryptoApiRandomGenerator()),
                    rsaLength
                )
            );

            return keypairGenerator.GenerateKeyPair();
        }
    }
}
