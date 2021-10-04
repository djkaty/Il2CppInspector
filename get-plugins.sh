#!/usr/bin/env bash

# Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
# All rights reserved.

# Download the latest set of plugins to a temporary file, extract the archive then delete it
wget https://github.com/djkaty/Il2CppInspectorPlugins/releases/latest/download/plugins.zip
unzip plugins.zip
rm plugins.zip

