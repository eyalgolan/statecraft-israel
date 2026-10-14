#!/usr/bin/env bash
# Kept for existing plans and scripts: runs tools/build_sim_for_unity.py, the single implementation.
exec python3 "$(dirname "$0")/build_sim_for_unity.py" "$@"
