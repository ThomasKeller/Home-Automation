#!/bin/bash
DIRECTORY="/git/services/iobroker.service.new"
DIRFILES_TO_REMOVE="$DIRECTORY/*"
#echo $DIRFILES_TO_REMOVE

if [ -d $DIRECTORY ]; then
  echo "$DIRECTORY does exist.";
else
  mkdir $DIRECTORY;
fi

rm $DIRFILES_TO_REMOVE

echo "build to $DIRECTORY:"
#dotnet publish -o /git/services/iobroker.service.new -c Release --self-contained --use-current-runtime
dotnet publish -o "$DIRECTORY" -c Release --use-current-runtime --no-self-contained
