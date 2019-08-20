using System;
using S2.BlackSwan.SupplyCollector.Models;
using Xunit;

namespace S3SupplyCollectorTests
{
    public class S3SupplyCollectorTests : IClassFixture<LaunchSettingsFixture>
    {
        private readonly S3SupplyCollector.S3SupplyCollector _instance;
        public readonly DataContainer _container;
        private LaunchSettingsFixture _fixture;

        public S3SupplyCollectorTests(LaunchSettingsFixture fixture)
        {
            _fixture = fixture;
            _instance = new S3SupplyCollector.S3SupplyCollector();
            _container = new DataContainer()
            {
                ConnectionString = _instance.BuildConnectionString(
                    Environment.GetEnvironmentVariable("S3_ACCESS_KEY"),
                    Environment.GetEnvironmentVariable("S3_SECREY_KEY"),
                    Environment.GetEnvironmentVariable("S3_REGION"),
                    Environment.GetEnvironmentVariable("S3_CONTAINER")
                    )
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

        [Fact]
        public void TestFilenamesInSchema()
        {
            var prefixCollector = new S3SupplyCollector.S3SupplyCollector("emails/2019/08", 0, true);
            var (tables, elements) = prefixCollector.GetSchema(_container);

            Assert.Equal(1, tables.Count);
            Assert.Equal(39, elements.Count);
            Assert.Equal("EMAILS-UTF8.CSV", tables[0].Name);

            var levelsCollector = new S3SupplyCollector.S3SupplyCollector(null, 1, false);
            (tables, elements) = levelsCollector.GetSchema(_container);

            Assert.Equal(1, tables.Count);
            Assert.Equal(39, elements.Count);
            Assert.Equal("emails", tables[0].Name);

            var noprefixCollector = new S3SupplyCollector.S3SupplyCollector(null, 1, true);
            (tables, elements) = noprefixCollector.GetSchema(_container);

            Assert.Equal(2, tables.Count);
            Assert.Equal(69, elements.Count);

            Assert.NotNull(tables.Find(x => x.Name.Equals("emails/EMAILS-UTF8.CSV")));
            Assert.NotNull(tables.Find(x => x.Name.Equals("emails/emails-utf8.parquet")));
        }

    }
}
