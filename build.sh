#!/bin/bash
echo "Running .NET Core restore."
dotnet restore
echo "Building Windows release."
dotnet publish -c Release --self-contained -r win10-x64 --output "../releases/windows"
echo "Building Ubuntu 16.10 release."
dotnet publish -c Release --self-contained -r ubuntu.16.10-x64 --output "../releases/linux"
echo "Building macOS release."
dotnet publish -c Release --self-contained -r osx.10.14-x64 --output "../releases/mac"
echo "Copying contents of the \"CPToBuild\" folder into each release."
cp -a ./CPToBuild/. ./releases/mac
cp -a ./CPToBuild/. ./releases/windows
cp -a ./CPToBuild/. ./releases/linux
