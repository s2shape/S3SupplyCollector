using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using DriveSupplyCollectorBase;
using S2.BlackSwan.SupplyCollector.Models;

namespace S3SupplyCollector
{
    public class S3SupplyCollector : DriveSupplyCollectorBase.DriveSupplyCollectorBase {
        private string _bucketName;

        public override List<string> DataStoreTypes() {
            return (new[] { "S3" }).ToList();
        }

        public string BuildConnectionString(string accessKey, string secretKey, string region, string bucket) {
            return $"s3://{accessKey}:{secretKey}@{region}/{bucket}";
        }

        private AmazonS3Client Connect(string connectString) {
            if(!connectString.StartsWith("s3://"))
                throw new ArgumentException("Invalid connection string!");
            var parts = connectString.Substring("s3://".Length).Split(new char[] {':', '@', '/'});

            if (parts.Length != 4) {
                throw new ArgumentException("Invalid connection string!");
            }

            _bucketName = parts[3];

            return new AmazonS3Client(parts[0], parts[1], RegionEndpoint.GetBySystemName(parts[2]));
        }

        public override bool TestConnection(DataContainer container) {
            try {
                var s3Client = Connect(container.ConnectionString);
                var buckets = s3Client.ListBucketsAsync().Result;
                return true;
            }
            catch (Exception) {
                return false;
            }
        }

        protected override List<DriveFileInfo> ListDriveFiles(DataContainer container) {
            var files = new List<DriveFileInfo>();

            var s3Client = Connect(container.ConnectionString);

            var request = new ListObjectsRequest();
            request.BucketName = _bucketName;
            ListObjectsResponse response = s3Client.ListObjects(request);
            foreach (S3Object o in response.S3Objects)
            {
                if(o.Size <= 0)
                    continue;
                
                files.Add(new DriveFileInfo() {
                    FilePath = o.Key,
                    FileSize = o.Size
                });
            }
            
            return files;
        }
    }
}
