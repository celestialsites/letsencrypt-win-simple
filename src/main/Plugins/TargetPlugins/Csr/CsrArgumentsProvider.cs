﻿using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    class CsrArgumentsProvider : BaseArgumentsProvider<CsrArguments>
    {
        public override string Name => "CSR plugin";
        public override string Group => "Target";
        public override string Condition => "--target csr";

        public override bool Active(CsrArguments current)
        {
            return !string.IsNullOrEmpty(current.CsrFile);
        }

        public override void Configure(FluentCommandLineParser<CsrArguments> parser)
        {
            parser.Setup(o => o.CsrFile)
                .As("csrfile")
                .WithDescription("Specify the location of a CSR file to make a certificate for");

            parser.Setup(o => o.PkFile)
                .As("pkfile")
                .WithDescription("Specify the location of the private key corresponding to the CSR");
        }
    }
}
