#!/usr/bin/python2.7

from __future__ import print_function
from subprocess import call
from distutils.dir_util import copy_tree
from os import chmod
# Handles the imports.

mk_release = lambda folder_name, arch: call(["dotnet", "publish", "-c", "Release", "--self-contained", "-r", arch, "--output", "../releases/" + folder_name])
# Makes the specified release.

print("Running .NET Core restore.")
call(["dotnet", "restore"])
# Restores the .NET Core package.

print("Building Windows release.")
mk_release("windows", "win10-x64")
# Builds a Windows release.

print("Building Ubuntu 16.10 release.")
mk_release("linux", "ubuntu.16.10-x64")
# Builds a Ubuntu 16.10 release.

print("Building macOS release.")
mk_release("mac", "osx.10.14-x64")
# Builds a macOS release.

folders = ["./releases/windows", "./releases/mac", "./releases/linux"]
# Defines all of the folders.

print("Copying contents of the \"CPToBuild\" folder into each release.")
for f in folders:
    copy_tree("./CPToBuild", f)
# Copies the contents of "CPToBuild" into each release.

print("Granting execution rights.")
chmod("./releases/mac/SuperServ", 0o777)
chmod("./releases/linux/SuperServ", 0o777)
# Grants execution rights to things that need it.
