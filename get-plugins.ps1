# Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
# All rights reserved.

# Download the latest set of plugins to a temporary file, extract the archive then delete it
$temp = New-TemporaryFile | Rename-Item -NewName { $_ -replace 'tmp$', 'zip' } –PassThru
wget -OutFile $temp https://github.com/djkaty/Il2CppInspectorPlugins/releases/latest/download/plugins.zip
Expand-Archive -Path $temp -DestinationPath $pwd -Force
del $temp
