﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.IO;

namespace PKISharp.WACS.UnitTests.Tests.InstallationPluginTests
{
    [TestClass]
    public class ArgumentParserTests
    {
        private Mock.Services.LogService log;

        public ArgumentParserTests()
        {
            log = new Mock.Services.LogService(true);
        }

        private string TestScript(string parameters)
        {
            log = new Mock.Services.LogService(true);
            var argParser = new ArgumentsParser(log,
                new PluginService(log),
                $"--scriptparameters {parameters} --verbose".Split(' '));
            var argService = new ArgumentsService(log, argParser);
            var args = argService.GetArguments<ScriptArguments>();
            return args.ScriptParameters;
        }

        [TestMethod]
        public void Illegal()
        {
            Assert.AreEqual(null, TestScript("hello nonsense"));
        }

        [TestMethod]
        public void SingleParam()
        {
            Assert.AreEqual("hello", TestScript("hello"));
        }

        [TestMethod]
        public void SingleParamExtra()
        {
            Assert.AreEqual("hello", TestScript("hello --verbose"));
        }

        [TestMethod]
        public void MultipleParams()
        {
            Assert.AreEqual("hello world", TestScript("\"hello world\""));
        }

        [TestMethod]
        public void MultipleParamsExtra()
        {
            Assert.AreEqual("hello world", TestScript("\"hello world\" --test --verbose"));
        }

        [TestMethod]
        public void MultipleParamsDoubleQuotes()
        {
            Assert.AreEqual("\"hello world\"", TestScript("\"\"hello world\"\""));
        }

        [TestMethod]
        public void MultipleParamsDoubleQuotesExtra()
        {
            Assert.AreEqual("\"hello world\"", TestScript("\"\"hello world\"\" --test --verbose"));
        }

        [TestMethod]
        public void MultipleParamsSingleQuotes()
        {
            Assert.AreEqual("'hello world'", TestScript("\"'hello world'\""));
        }


        [TestMethod]
        public void EmbeddedKeySingle()
        {
            Assert.AreEqual("'hello --world'", TestScript("\"'hello --world'\""));
        }

        [TestMethod]
        public void EmbeddedKeyDouble()
        {
            Assert.AreEqual("\"hello --world\"", TestScript("\"\"hello --world\"\""));
        }
    }
}
