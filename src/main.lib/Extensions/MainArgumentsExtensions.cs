﻿using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Extensions
{
    public static class MainArgumentsExtensions
    {
        /// <summary>
        /// Reset the options for a(nother) run through the main menu
        /// </summary>
        /// <param name="options"></param>
        public static void Clear(this MainArguments options)
        {
            options.Target = null;
            options.Renew = false;
            options.FriendlyName = null;
            options.Force = false;
            options.List = false;
            options.Version = false;
            options.Help = false;
            options.Id = null;
        }
    }
}
