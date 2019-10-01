﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DnsClient;
using DnsClient.Protocol;
using PKISharp.WACS.Services;
using Serilog.Context;

namespace PKISharp.WACS.Clients.DNS
{
    public class LookupClientWrapper
    {
        private readonly ILogService _log;
        private readonly DomainParseService _domainParser;
        private readonly LookupClientProvider _provider;
        public ILookupClient LookupClient { get; private set; }
        public IPAddress IpAddress { get; private set; }

        public LookupClientWrapper(DomainParseService domainParser, ILogService logService, IPAddress ipAddress, LookupClientProvider provider)
        {
            IpAddress = ipAddress;
            if (ipAddress == null)
            {
                LookupClient = new LookupClient();
            }
            else
            {
                LookupClient = new LookupClient(ipAddress);
            }
            LookupClient.UseCache = false;
            _log = logService;
            _domainParser = domainParser;
            _provider = provider;
        }

        public string GetRootDomain(string domainName)
        {
            return _domainParser.GetRegisterableDomain(domainName.TrimEnd('.'));
        }

        public IEnumerable<IPAddress> GetAuthoritativeNameServers(string domainName, int round)
        {
            domainName = domainName.TrimEnd('.');
            _log.Debug("Querying name servers for {part}", domainName);
            var nsResponse = LookupClient.Query(domainName, QueryType.NS);
            var nsRecords = nsResponse.Answers.NsRecords();
            if (!nsRecords.Any())
            {
                nsRecords = nsResponse.Authorities.OfType<NsRecord>();   
            }
            if (nsRecords.Any())
            {
                return GetNameServerIpAddresses(nsRecords.Select(n => n.NSDName.Value), round);
            }
            return null;
        }

        private IEnumerable<IPAddress> GetNameServerIpAddresses(IEnumerable<string> nsRecords, int round)
        {
            foreach (var nsRecord in nsRecords)
            {
                using (LogContext.PushProperty("NameServer", nsRecord))
                {
                    _log.Debug("Querying IP for name server");
                    var aResponse = _provider.GetDefaultClient(round).LookupClient.Query(nsRecord, QueryType.A);
                    var nameServerIp = aResponse.Answers.ARecords().FirstOrDefault()?.Address;
                    _log.Debug("Name server IP {NameServerIpAddress} identified", nameServerIp);
                    yield return nameServerIp;
                }
            }
        }

        public IEnumerable<string> GetTextRecordValues(string challengeUri, int attempt)
        {
            IDnsQueryResponse result = LookupClient.Query(challengeUri, QueryType.TXT);
            result = RecursivelyFollowCnames(result, attempt);

            return result.Answers.TxtRecords().
                Select(txtRecord => txtRecord?.EscapedText?.FirstOrDefault()).
                Where(txtRecord => txtRecord != null).
                ToList();
        }

        private IDnsQueryResponse RecursivelyFollowCnames(IDnsQueryResponse result, int attempt)
        {
            if (result.Answers.CnameRecords().Any())
            {
                var cname = result.Answers.CnameRecords().First();
                var recursiveClient = _provider.GetClient(cname.CanonicalName, attempt);
                IDnsQueryResponse txtResponse = recursiveClient.LookupClient.Query(cname.CanonicalName, QueryType.TXT);
                _log.Debug("Name server {NameServerIpAddress} selected", txtResponse.NameServer.Endpoint.Address.ToString());
                return recursiveClient.RecursivelyFollowCnames(txtResponse, attempt);
            }
            return result;
        }
    }
}