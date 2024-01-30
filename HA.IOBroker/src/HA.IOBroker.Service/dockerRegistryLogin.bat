REM docker build -t iobroker.service -f Dockerfile ..
REM docker tag 66e05a980e9a tkregistry.azurecr.io/iobroker.servive:1.0.1
docker login -u tkeller69 -p SEE_DESKTOP tkregistry.azurecr.io
REM docker tag iobroker.servive:1.0.1 tkregistry.azurecr.io/iobroker.servive:1.0.1
REM docker push tkregistry.azurecr.io/iobroker.servive:1.0.1
REM docker pull tkregistry.azurecr.io/iobroker.servive:1.0.1