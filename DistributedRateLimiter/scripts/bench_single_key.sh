#!/usr/bin/env bash
set -euo pipefail

# requires: hey (https://github.com/rakyll/hey)
URL=${URL:-http://localhost:8080/api/data}
N=${N:-2000}
C=${C:-100}

echo "Running single-key benchmark: $URL  (n=$N, c=$C)"
hey -n "$N" -c "$C" "$URL"
