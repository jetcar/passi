using System;
using System.Collections.Generic;
using AppCommon;
using ConfigurationManager;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Serilog.Core;

namespace ConfigurationManagerTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var myConfiguration = new Dictionary<string, string>
            {
                {"AppSetting:DbName", "Passi"},
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();
            var appSetting = new AppSetting(config);
            appSetting.PrefferAppsettingFile = true;
            Assert.AreEqual("Passi", appSetting["DbName"]);
        }

        [Test]
        public void DateTimeToTimeStampAndBack()
        {
            var datetime = DateTime.UtcNow;
            var timestamp = datetime.ToTimestamp();

            var fromTimestamp = timestamp.ToDateTime();
            Assert.AreEqual(datetime.Hour, fromTimestamp.Hour);
            Assert.AreEqual(datetime.Minute, fromTimestamp.Minute);
            Assert.AreEqual(datetime.Second, fromTimestamp.Second);
        }
    }
}