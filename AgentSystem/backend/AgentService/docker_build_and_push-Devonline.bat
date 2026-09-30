@echo off

dotnet publish -c Release -o bin/Release/Staging
docker build -f Dockerfile_Devonline -t hub.pushsale.vn/scrm/scrmagentsystem.api:staging-1.0 .
docker push hub.pushsale.vn/scrm/scrmagentsystem.api:staging-1.0

rem curl -X POST http://10.0.161.22:9000/api/webhooks/2d40a348-655a-4ffd-895b-0633c986a620

pause