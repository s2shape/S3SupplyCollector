using System;
using S2.BlackSwan.SupplyCollector.Models;
using Xunit;

namespace S3SupplyCollectorTests
{
    public class S3SupplyCollectorTests
    {
        private readonly S3SupplyCollector.S3SupplyCollector _instance;
        public readonly DataContainer _container;

        public S3SupplyCollectorTests()
        {
            _instance = new S3SupplyCollector.S3SupplyCollector();
            _container = new DataContainer()
            {
                ConnectionString = _instance.BuildConnectionString("", "", "us-east-1", "itsplus")
            };
        }

        [Fact]
        public void DataStoreTypesTest()
        {
            var result = _instance.DataStoreTypes();
            Assert.Contains("S3", result);
        }

        [Fact]
        public void TestConnectionTest()
        {
            var result = _instance.TestConnection(_container);
            Assert.True(result);
        }

    }
}
