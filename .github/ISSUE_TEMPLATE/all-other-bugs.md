---
name: All other bugs
about: Any bug in Il2CppInspector not relating to loading IL2CPP files
title: ''
labels: bug
assignees: ''

---

**BEFORE YOU SUBMIT AN ISSUE** follow these steps:

1. Make sure you are using the latest commit of Il2CppInspector, which is usually ahead of the latest pre-compiled release
2. Make sure you have the latest set of plugins by using `get-plugins.ps1` or `get-plugins.sh`
3. If using the GUI, make sure all Il2CppInspector Core Plugins are enabled and all other plugins are disabled

To submit an issue relating to a crash or error message when processing files or generating outputs:

1. We require the output from the Il2CppInspector CLI. Include the complete command line with all arguments and paste the entire output.
2. Attach `global-metadata.dat` and the IL2CPP binary to the issue.

To submit an issue for behavioral bugs (eg. incorrect outputs, options not working as expected, broken features etc.):

1. Provide a clear and concise description of the bug.
2. Provide the exact steps you took to produce the bug, so that we can reproduce it ourselves.

Issues not meeting these criteria will be automatically closed and tagged as invalid.
