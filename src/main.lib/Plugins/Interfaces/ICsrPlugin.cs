﻿using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface ICsrPlugin : IPlugin
    {
        Task<AsymmetricCipherKeyPair> GetKeys();
        Task<Pkcs10CertificationRequest> GenerateCsr(string cacheFile, string commonName, List<string> identifiers);
        Task<X509Certificate2> PostProcess(X509Certificate2 certificate);
    }
}
