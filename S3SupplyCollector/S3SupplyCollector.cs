using System;
using System.Collections.Generic;
using System.IO;
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

            var keyIndex = "s3://".Length;
            var secretIndex = connectString.IndexOf(":", keyIndex);
            if (secretIndex <= 0)
                throw new ArgumentException("Invalid connection string!");
            var regionIndex = connectString.IndexOf("@", secretIndex);
            if (regionIndex <= 0)
                throw new ArgumentException("Invalid connection string!");
            var bucketIndex = connectString.IndexOf("/", regionIndex);
            if (bucketIndex <= 0)
                throw new ArgumentException("Invalid connection string!");

            var accessKey = connectString.Substring(keyIndex, secretIndex - keyIndex);
            var secretKey = connectString.Substring(secretIndex + 1, regionIndex - secretIndex - 1);
            var region = connectString.Substring(regionIndex + 1, bucketIndex - regionIndex - 1);
            _bucketName = connectString.Substring(bucketIndex + 1);

            return new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(region));
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

        protected override Stream GetFileStream(DataContainer container, string filePath) {
            var s3Client = Connect(container.ConnectionString);
            var response = s3Client.GetObjectAsync(_bucketName, filePath).Result;

            return response.ResponseStream;
        }

        protected override List<DriveFileInfo> ListDriveFiles(DataContainer container) {
            var files = new List<DriveFileInfo>();

            var s3Client = Connect(container.ConnectionString);

            var request = new ListObjectsRequest();
            request.BucketName = _bucketName;
            ListObjectsResponse response = s3Client.ListObjectsAsync(request).Result;
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
