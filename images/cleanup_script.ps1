$time_mark_file="${env:TMP}\time_mark"
if (!(Test-Path $time_mark_file -PathType Leaf)) {
  New-Item -Path $time_mark_file -ItemType File
}

# clean cache every week
$create_time = (Get-Item $time_mark_file).CreationTime
if ($create_time -lt (Get-Date).AddDays(-7).DateTime) {
  echo "run clean"
  Remove-Item $CARGO_HOME/registry -Recurse -Force
  Remove-Item $CARGO_HOME/git -Recurse -Force
  Remove-Item $RUSTUP_HOME/downloads -Recurse -Force
  Remove-Item $RUSTUP_HOME/tmp -Recurse -Force
  Remove-Item $RUSTUP_HOME/toolchains -Recurse -Force
  Remove-Item $RUSTUP_HOME/update-hashes -Recurse -Force
  Remove-Item $PNPM_CACHE -Recurse -Force

  Remove-Item $time_mark_file -Force
  New-Item -Path $time_mark_file -ItemType File
}
