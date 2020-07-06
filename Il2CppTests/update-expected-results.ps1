# Copyright 2019-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
# All rights reserved.

# This script copies all the test results from TestBinaries into TestExpectedResults with the correct filenames
# The idea is to update all the expected results once we have confirmed an improvement to the codebase works correctly to save a lot of manual copying
# It is only intended to be used during development, not for end-user scenarios

# Get all test results
$bin = (gci "$PSScriptRoot/TestBinaries/*/*" -Filter test-result.cs)
$bin2 = (gci "$PSScriptRoot/TestBinaries/*/*" -Filter test-result-1.cs)
$py = (gci "$PSScriptRoot/TestBinaries/*/*" -Filter test-ida-result.py)
$py2 = (gci "$PSScriptRoot/TestBinaries/*/*" -Filter test-ida-result-1.py)
$cpp = (gci "$PSScriptRoot/TestBinaries/*/*" -Filter test-result.h)
$cpp2 = (gci "$PSScriptRoot/TestBinaries/*/*" -Filter test-result-1.h)

# Get path to expected test results
$results = "$PSScriptRoot/TestExpectedResults"

$bin | % {
	$target = $results + "/" + (Split-Path -Path (Split-Path -Path $_) -Leaf) + ".cs"
	cp $_ -Destination $target -Force
}

$bin2 | % {
	$target = $results + "/" + (Split-Path -Path (Split-Path -Path $_) -Leaf) + "-1.cs"
	cp $_ -Destination $target -Force
}

$py | % {
	$target = $results + "/" + (Split-Path -Path (Split-Path -Path $_) -Leaf) + ".py"
	cp $_ -Destination $target -Force
}

$py2 | % {
	$target = $results + "/" + (Split-Path -Path (Split-Path -Path $_) -Leaf) + "-1.py"
	cp $_ -Destination $target -Force
}

$cpp | % {
	$target = $results + "/" + (Split-Path -Path (Split-Path -Path $_) -Leaf) + ".h"
	cp $_ -Destination $target -Force
}

$cpp2 | % {
	$target = $results + "/" + (Split-Path -Path (Split-Path -Path $_) -Leaf) + "-1.h"
	cp $_ -Destination $target -Force
}