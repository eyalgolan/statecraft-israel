### Fixed
- `scripts/check.sh`'s Unity compile step now finds a Linux editor (`~/Unity/Hub/Editor/<version>/Editor/Data`), and
  `scripts/unity_compile/compile.sh` accepts that layout, so the check no longer always skips on Linux. macOS is unchanged.
