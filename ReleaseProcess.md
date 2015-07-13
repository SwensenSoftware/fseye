# Note #

This processes has been adapted from Unquote's release process: https://code.google.com/p/unquote/wiki/ReleaseProcess

# Assumptions #

The latest revision in trunk is ready for release and all unit tests pass.

# Build #

  1. `svn checkout` the head revision from [trunk](https://fseye.googlecode.com/svn/trunk)
  1. Update version numbers (`<monumental>.<significant>.<minor or bug fix>`) and commit changes to trunk
    * `AssemblyVersion` and `AssemblyFileVersion` in `\FsEye\AssemblyInfo.fs`
  1. Build the Visual Studio 2010 solution in Release mode.
  1. Run package.bat
    1. Enter the version number when prompted
    1. Check the output for any errors
    1. The Downloads zip will be located at `\builds\FsEye-<version>.zip`
  1. Perform quality assurance
    * Unzip and test via the FsEye.fsx script in the F# interactive of at least Visual Studio 2010

# Release #
  * Update the milestone for all issues which are being included in the release to the proper version number
    1. Search within `All issues` for `-has:milestone status:fixed`
    1. Click `Select: All` to select all of them
    1. Choose "Bulk edit" action from the "Actions..." drop-down list
    1. Add the label `Milestone-Release<version number>`
    1. Click "Update ## Issues" button to save
  * Update the ReleaseNotes
    1. Search within `All issues` for `milestone:Release<version>`
    1. Click the "CSV" link in the bottom right-hand corner of the issue list to export the issues to a csv file
    1. Open the csv file in Excel
    1. Delete all columns except for the ID and Summary columns from the csv
    1. Open up the ReleaseNotes wiki page for edit
    1. Copy and paste the records from the csv into the ReleaseNotes following the existing formatting conventions and save
  * Upload zip (now hosted by Swensen Software)
    1. TODO

# Finalize #

Tag the head revision of the trunk with the name `<version number>`.

# Process Improvement #

Eliminate manual Visual Studio 2010 build step, this should be automated with a script.