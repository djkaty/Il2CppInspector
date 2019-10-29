# Copyright 2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com
# All rights reserved.

# This script copies all the test results from TestBinaries into TestExpectedResults with the correct filenames
# The idea is to update all the expected results once we have confirmed an improvement to the codebase works correctly to save a lot of manual copying
# It is only intended to be used during development, not for end-user scenarios

# Get all test results
$bin = (gci "$PSScriptRoot/TestBinaries/*/*" -Filter test-result.cs)
$bin2 = (gci "$PSScriptRoot/TestBinaries/*/*" -Filter test-result-1.cs)

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