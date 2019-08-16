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

        [Fact]
        public void TestSchemaTest()
        {
            var (tables, elements) = _instance.GetSchema(_container);

            Assert.Equal(1, tables.Count);
            
            Assert.NotNull(elements.Find(x => x.Name.Equals("FROM_NAME")));
        }

        [Fact]
        public void TestCollectSamplesTest()
        {
            var entity = new DataEntity("FROM_ADDR", DataType.String, "String", _container, new DataCollection(_container, "EMAILS-UTF8.CSV") );
            var samples = _instance.CollectSample(entity, 5);
            Assert.Equal(5, samples.Count);
            Assert.Contains("sally@example.com", samples);

        }

    }
}
