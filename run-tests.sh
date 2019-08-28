#!/bin/bash
docker run -d -p 9000:9000 --name minio1 -e "MINIO_ACCESS_KEY=user" -e "MINIO_SECRET_KEY=password" --mount source=$(pwd)/S3SupplyCollectorTests/tests,target=/data,type=bind minio/minio server /data 

echo { > S3SupplyCollectorTests/Properties/launchSettings.json
echo   \"profiles\": { >> S3SupplyCollectorTests/Properties/launchSettings.json
echo     \"S3SupplyCollectorTests\": { >> S3SupplyCollectorTests/Properties/launchSettings.json
echo       \"commandName\": \"Project\", >> S3SupplyCollectorTests/Properties/launchSettings.json
echo       \"environmentVariables\": { >> S3SupplyCollectorTests/Properties/launchSettings.json
echo         \"S3_ACCESS_KEY\": \"user\", >> S3SupplyCollectorTests/Properties/launchSettings.json
echo         \"S3_SECRET_KEY\": \"pass\", >> S3SupplyCollectorTests/Properties/launchSettings.json
echo         \"S3_REGION\": \"us-east-1\", >> S3SupplyCollectorTests/Properties/launchSettings.json
echo         \"S3_CONTAINER\": \"bucket\" >> S3SupplyCollectorTests/Properties/launchSettings.json
echo         \"S3_HOST\": \"http://localhost:9000\" >> S3SupplyCollectorTests/Properties/launchSettings.json
echo       } >> S3SupplyCollectorTests/Properties/launchSettings.json
echo     } >> S3SupplyCollectorTests/Properties/launchSettings.json
echo   } >> S3SupplyCollectorTests/Properties/launchSettings.json
echo } >> S3SupplyCollectorTests/Properties/launchSettings.json

dotnet build
dotnet test
rem docker stop minio1
rem docker rm minio1
