#!/usr/bin/env bash
set -euo pipefail

URL=${URL:-http://localhost:5000/api/data}
N=${N:-50000}
C=${C:-200}

echo "Running single-key benchmark: $URL  (n=$N, c=$C)"
hey -n "$N" -c "$C" "$URL"

# 1000 requests, 50 concurrent workers, same key "user1"
#hey -n 1000 -c 50 \
#  -m GET \
#  "http://localhost:8080/api/data?user=user1"

# Generate 100 users, each gets traffic
#for i in $(seq 1 100); do
#  hey -n 200 -c 20 \
#    -m GET \
#    "http://localhost:8080/api/data?user=user$i" &
#done
#wait

#hey -n 20000 -c 100 -m GET \
#  "http://localhost:8080/api/data?user=$(jot -r 1 1 200000)"

#hey -n 1000 -c 100 -z 30s -host localhost -disable-keepalive < urls.txt


