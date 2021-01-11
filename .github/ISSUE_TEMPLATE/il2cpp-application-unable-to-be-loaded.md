---
name: IL2CPP application unable to be loaded
about: Errors when trying to load IL2CPP applications into Il2CppInspector
title: ''
labels: bug
assignees: ''

---

**BEFORE YOU SUBMIT AN ISSUE** follow these steps:

1. Make sure you are using the latest commit of Il2CppInspector, which is usually ahead of the latest pre-compiled release
2. Make sure you have the latest set of plugins by using `get-plugins.ps1` or `get-plugins.sh`
3. Make sure all Il2CppInspector Core Plugins are enabled and all other plugins are disabled
4. Open `global-metadata.dat` and the binary in a hex editor and confirm they are not obfuscated or encrypted in a way not currently supported by Il2CppInspector

To submit an issue:

1. We require the output from the Il2CppInspector CLI. Include the complete command line with all arguments and paste the entire output.
2. Attach `global-metadata.dat` and the IL2CPP binary to the issue.
3. If the files have been modified or extracted by a 3rd party tool, note this in the issue.

Issues not meeting these criteria will be automatically closed and tagged as invalid.
