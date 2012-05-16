# Clean.ps1
#
# This little script just removes all the bin and obj subdirectories 
# in the various DotNetZip project directoties. This allows cleanest zip-up 
# of the source.
# 
# last saved Time-stamp: <Thursday, May 29, 2008  23:31:39  (by dinoch)>
#
#
#


get-childItem -recurse |? {$_.Name -eq "bin"} | remove-item -recurse
get-childItem -recurse |? {$_.Name -eq "obj"} | remove-item -recurse
get-childItem -recurse |? {$_.Name -eq "TestResults"} | remove-item -recurse
get-childitem -recurse |? {$_.Name -eq "LastBuild.log"} | remove-item
get-childItem -recurse |? {$_.Name -eq "Debug"} | remove-item -recurse
