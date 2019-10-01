﻿using PKISharp.WACS.DomainObjects;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// Does the actual work
    /// </summary>
    public interface IInstallationPlugin
    {
        /// <summary>
        /// Do the installation work
        /// </summary>
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="newCertificateInfo"></param>
        /// <param name="oldCertificateInfo"></param>
        void Install(IEnumerable<IStorePlugin> stores, CertificateInfo newCertificateInfo, CertificateInfo oldCertificateInfo);
    }
}
