ECHO %API_KEY% 
ECHO %VERSION%
dotnet nuget push .\artifacts\HA.Common.%VERSION%.nupkg --api-key %API_KEY% --source https://api.nuget.org/v3/index.json