using System.Security.Cryptography.X509Certificates;

namespace BackseatCommanderMod.Server
{
    internal interface IServerCertificateGenerator
    {
        X509Certificate2 GenerateNew();
    }
}
