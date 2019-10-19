using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using S2.BlackSwan.SupplyCollector.Models;
using SupplyCollectorDataLoader;

namespace S3SupplyCollectorLoader
{
    public class S3SupplyCollectorLoader : SupplyCollectorDataLoaderBase
    {
        private string _container;
        private string _overrideHost;
        protected string _s2Prefix;
        protected int _s2FolderLevels = 0;
        protected bool _s2UseFileNameInDcName = false;
        protected bool _csvHasHeader = true;
        private string _bucketName;

        private const string PREFIX = "s3://";

        private void ParseConnectionStringAdditions(string additions)
        {
            var parts = additions.Split(",");
            foreach (var part in parts)
            {
                if (String.IsNullOrEmpty(part))
                    continue;

                var pair = part.Split("=");
                if (pair.Length == 2)
                {
                    if ("s2-prefix".Equals(pair[0]))
                    {
                        _s2Prefix = pair[1];
                    }
                    else if ("s2-folder-levels-used-in-dc-name".Equals(pair[0]))
                    {
                        _s2FolderLevels = Int32.Parse(pair[1]);
                    }
                    else if ("s2-use-file-name-in-dc-name".Equals(pair[0]))
                    {
                        _s2UseFileNameInDcName = Boolean.Parse(pair[1]);
                    }
                    else if ("csv_has_header".Equals(pair[0]))
                    {
                        _csvHasHeader = Boolean.Parse(pair[1]);
                    }
                    else if ("override_host".Equals(pair[0]))
                    {
                        _overrideHost = pair[1];
                    }
                }
            }
        }

        private AmazonS3Client Connect(string connectString)
        {
            if (!connectString.StartsWith(PREFIX))
                throw new ArgumentException("Invalid connection string!");

            var keyIndex = PREFIX.Length;
            var secretIndex = connectString.IndexOf(":", keyIndex);
            if (secretIndex <= 0)
                throw new ArgumentException("Invalid connection string!");
            var regionIndex = connectString.IndexOf("@", secretIndex);
            if (regionIndex <= 0)
                throw new ArgumentException("Invalid connection string!");
            var bucketIndex = connectString.IndexOf("/", regionIndex);
            if (bucketIndex <= 0)
                throw new ArgumentException("Invalid connection string!");
            var additionsIndex = connectString.IndexOf(",", bucketIndex);

            var accessKey = connectString.Substring(keyIndex, secretIndex - keyIndex);
            var secretKey = connectString.Substring(secretIndex + 1, regionIndex - secretIndex - 1);
            var region = connectString.Substring(regionIndex + 1, bucketIndex - regionIndex - 1);

            if (additionsIndex > 0)
            {
                _bucketName = connectString.Substring(bucketIndex + 1, additionsIndex - bucketIndex - 1);
                ParseConnectionStringAdditions(connectString.Substring(additionsIndex + 1));
            }
            else
            {
                _bucketName = connectString.Substring(bucketIndex + 1);
            }

            if (_overrideHost != null) {
                var config = new AmazonS3Config();
                config.ServiceURL = _overrideHost;
                config.ForcePathStyle = true;

                Console.WriteLine($"Constructing AmazonS3 client for {_overrideHost}");
                return new AmazonS3Client(accessKey, secretKey, config);
            }

            return new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(region));
        }


        public override void InitializeDatabase(DataContainer dataContainer) {
            try {
                Console.WriteLine("... connecting");
                var client = Connect(dataContainer.ConnectionString);
                Console.WriteLine("... listing buckets");
                var bucketList = client.ListBucketsAsync().Result;
                var bucket = bucketList.Buckets.FirstOrDefault(x => x.BucketName.Equals(_bucketName));
                Console.WriteLine($"... existing bucket = {bucket}");
                if (bucket == null)
                    client.PutBucketAsync(_bucketName).Wait();
            }
            catch (Exception ex) {
                Console.WriteLine($"{ex}");
                throw ex;
            }
        }

        public override void LoadSamples(DataEntity[] dataEntities, long count) {
            Console.Write("... generating samples: ");
            var path = Path.GetTempFileName();
            using (var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8)) {
                if (_csvHasHeader) {
                    writer.WriteLine(String.Join(", ", dataEntities.Select(x => x.Name).ToArray()));
                }

                var r = new Random();
                long rows = 0;
                while (rows < count) {
                    if (rows % 1000 == 0)
                        Console.Write(".");

                    bool first = true;
                    foreach (var dataEntity in dataEntities) {
                        if (!first)
                            writer.Write(", ");

                        switch (dataEntity.DataType) {
                            case DataType.String:
                                writer.Write(Guid.NewGuid().ToString());
                                break;
                            case DataType.Int:
                                writer.Write(r.Next().ToString());
                                break;
                            case DataType.Double:
                                writer.Write(r.NextDouble().ToString().Replace(",", "."));
                                break;
                            case DataType.Boolean:
                                writer.Write(r.Next(100) > 50 ? "true" : "false");
                                break;
                            case DataType.DateTime:
                                var val = DateTimeOffset
                                    .FromUnixTimeMilliseconds(
                                        DateTimeOffset.Now.ToUnixTimeMilliseconds() + r.Next()).DateTime;

                                writer.Write(val.ToString("s"));
                                break;
                            default:
                                writer.Write(r.Next().ToString());
                                break;
                        }

                        first = false;
                    }

                    writer.WriteLine();
                    rows++;
                }
            }

            Console.WriteLine();

            Console.WriteLine("... uploading to S3");

            var client = Connect(dataEntities[0].Container.ConnectionString);

            string remotePath;
            if (_s2UseFileNameInDcName) {
                remotePath = _s2Prefix ?? "" + "/" + dataEntities[0].Collection.Name + "/" +
                             dataEntities[0].Collection.Name + ".csv";
            }
            else {
                remotePath = _s2Prefix ??
                             "" + "/" + dataEntities[0].Collection.Name + "/" + Guid.NewGuid() + ".csv";
            }

            client.PutObjectAsync(new PutObjectRequest() {
                FilePath = path,
                Key = remotePath,
                BucketName = _bucketName
            }).Wait();

            File.Delete(path);
        }

        private string[] ListTestFiles(string path, string root = null)
        {
            Console.WriteLine($"ListTestFiles({path}, {root})");

            var list = new List<string>();

            var dirs = Directory.GetDirectories(path);
            foreach (var dir in dirs)
            {
                if (dir.Equals(".") || dir.Equals(".."))
                    continue;

                Console.WriteLine($"... found dir={dir}");
                var dirName = Path.GetFileName(dir);

                list.AddRange(ListTestFiles($"{path}/{dirName}", (root == null ? "" : (root + "/")) + dirName));
            }

            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                list.Add($"{root}/{fileName}");
            }

            return list.ToArray();
        }

        public override void LoadUnitTestData(DataContainer dataContainer) {
            var client = Connect(dataContainer.ConnectionString);
            var files = ListTestFiles("tests");
            foreach (var file in files) {
                Console.WriteLine($"... uploading tests/{file} to {file}");
                client.PutObjectAsync(new PutObjectRequest() {
                    FilePath = $"tests/{file}",
                    Key = file,
                    BucketName = _bucketName
                }).Wait();
            }
        }
    }
}
