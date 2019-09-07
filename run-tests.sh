#!/bin/bash
docker run -d -p 9000:9000 --name minio1 -e "MINIO_ACCESS_KEY=user" -e "MINIO_SECRET_KEY=password" --mount source=$(pwd)/S3SupplyCollectorTests/tests,target=/data,type=bind minio/minio server /data 

export S3_ACCESS_KEY=user
export S3_SECRET_KEY=password
export S3_REGION=us-east-1
export S3_CONTAINER=bucket
export S3_HOST=http://localhost:9000

dotnet build
dotnet test
docker stop minio1
docker rm minio1
