#!/bin/bash
WWWROOT_LOCATION="./bin/Debug/net6.0/publish/wwwroot/"

echo "Transpiling the proxy libraries"
dotnet publish 1> /dev/null

if [ "$?" -ne "0" ]; then
    echo "Failed to transpile the proxy libraries"
    exit 1
fi

echo "Starting up the container project"
python -m http.server -d $WWWROOT_LOCATION