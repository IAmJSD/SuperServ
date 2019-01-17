#!/usr/bin/python2.7

from __future__ import print_function
from subprocess import call
from distutils.dir_util import copy_tree
from os import chmod
# Handles the imports.

print("Running .NET Core restore.")
call(["dotnet", "restore"])
# Restores the .NET Core package.

mk_release = lambda folder_name, arch: call(["dotnet", "publish", "-c", "Release", "--self-contained", "-r", arch, "--output", "../releases/" + folder_name])
# Makes the specified release.

releases = {
    "windows_x64": ["Windows x64", "win10-x64"],
    "windows_x86": ["Windows x86", "win10-x86"],
    "linux_x64": ["Ubuntu 16.10 x64", "ubuntu.16.10-x64"],
    "linux_arm": ["Ubuntu 16.04 ARM", "ubuntu.16.04-arm"],
    "mac": ["macOS", "osx.10.14-x64"]
}
# Defines all of the releases

for r in releases:
    r_info = releases[r]
    nice_name = r_info[0]
    arch = r_info[1]
    print("Building %s release." % nice_name)
    mk_release(r, arch)
# Handles all of the releases.

release_folders = ["./releases/%s" % r for r in releases]
# Defines all of the release folders.

print("Copying contents of the \"CPToBuild\" folder into each release.")
for f in release_folders:
    copy_tree("./CPToBuild", f)
# Copies the contents of "CPToBuild" into each release.

print("Granting execution rights.")
chmod("./releases/mac/SuperServ", 0o777)
chmod("./releases/linux_x64/SuperServ", 0o777)
chmod("./releases/linux_arm/SuperServ", 0o777)
# Grants execution rights to things that need it.
