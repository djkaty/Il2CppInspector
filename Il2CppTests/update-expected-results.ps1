# Copyright 2019-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
# All rights reserved.

# This script copies all the test results from TestBinaries into TestExpectedResults with the correct filenames
# The idea is to update all the expected results once we have confirmed an improvement to the codebase works correctly to save a lot of manual copying
# It is only intended to be used during development, not for end-user scenarios

# Get all test results
echo "Initializing"

$cs = (gci "$PSScriptRoot/TestBinaries/*/*" -Filter test-result.cs)
$cs2 = (gci "$PSScriptRoot/TestBinaries/*/*" -Filter test-result-1.cs)
$json = (gci "$PSScriptRoot/TestBinaries/*/*" -Filter test-result.json)
$json2 = (gci "$PSScriptRoot/TestBinaries/*/*" -Filter test-result-1.json)
$cpp = (gci "$PSScriptRoot/TestBinaries/*/test-cpp-result/appdata/*" -Filter il2cpp-types.h)
$cpp2 = (gci "$PSScriptRoot/TestBinaries/*/test-cpp-result-1/appdata/*" -Filter il2cpp-types.h)
$dll = (gci "$PSScriptRoot/TestBinaries/*/test-dll-result/*")
$dll2 = (gci "$PSScriptRoot/TestBinaries/*/test-dll-result-1/*")

# Get path to expected test results
$results = "$PSScriptRoot/TestExpectedResults"

# Wipe existing results
echo "Removing previous results"

rm -Recurse -Force $results >$null 2>&1
mkdir $results >$null 2>&1

echo "Updating .cs files"

$cs | % {
	$target = $results + "/" + (Split-Path -Path (Split-Path -Path $_) -Leaf) + ".cs"
	cp $_ -Destination $target -Force
}

$cs2 | % {
	$target = $results + "/" + (Split-Path -Path (Split-Path -Path $_) -Leaf) + "-1.cs"
	cp $_ -Destination $target -Force
}

echo "Updating .json files"

$json | % {
	$target = $results + "/" + (Split-Path -Path (Split-Path -Path $_) -Leaf) + ".json"
	cp $_ -Destination $target -Force
}

$json2 | % {
	$target = $results + "/" + (Split-Path -Path (Split-Path -Path $_) -Leaf) + "-1.json"
	cp $_ -Destination $target -Force
}

echo "Updating .h files"

$cpp | % {
	$target = $results + "/" + (Split-Path -Path (Split-Path -Path (Split-Path -Path (Split-Path -Path $_))) -Leaf) + ".h"
	cp $_ -Destination $target -Force
}

$cpp2 | % {
	$target = $results + "/" + (Split-Path -Path (Split-Path -Path (Split-Path -Path (Split-Path -Path $_))) -Leaf) + "-1.h"
	cp $_ -Destination $target -Force
}

echo "Updating .dll files"

$dll | % {
	$targetPath = $results + "/dll-" + (Split-Path -Path (Split-Path -Path (Split-Path -Path $_)) -Leaf)
	mkdir $targetPath >$null 2>&1
	$target =  $targetPath + "/" + (Split-Path -Path $_ -Leaf)
	cp $_ -Destination $target -Force
}

$dll2 | % {
	$targetPath = $results + "/dll-" + (Split-Path -Path (Split-Path -Path (Split-Path -Path $_)) -Leaf) + "-1"
	mkdir $targetPath >$null 2>&1
	$target =  $targetPath + "/" + (Split-Path -Path $_ -Leaf)
	cp $_ -Destination $target -Force
}
