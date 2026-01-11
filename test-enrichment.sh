#!/bin/bash
set -e

# Get script directory (project root)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# Configuration
PRODUCT_FILE="${1:-largeSizeProduct.csv}"
TRADE_FILE="${2:-middleSizeTrade.csv}"
OUTPUT_FILE="enriched-output.csv"
PORT=5019
MAX_SIZE=268435456  # 256 MB

# Convert to absolute paths
PRODUCT_FILE_ABS="$(cd "$(dirname "$PRODUCT_FILE")" && pwd)/$(basename "$PRODUCT_FILE")"
TRADE_FILE_ABS="$(cd "$(dirname "$TRADE_FILE")" && pwd)/$(basename "$TRADE_FILE")"

echo "=== Trade Enrichment Test ==="
echo "Product file: $PRODUCT_FILE_ABS"
echo "Trade file:   $TRADE_FILE_ABS"
echo ""

# Check files exist
if [[ ! -f "$PRODUCT_FILE_ABS" ]]; then
    echo "Error: Product file not found: $PRODUCT_FILE_ABS"
    exit 1
fi
if [[ ! -f "$TRADE_FILE_ABS" ]]; then
    echo "Error: Trade file not found: $TRADE_FILE_ABS"
    exit 1
fi

# Start the API in background
echo "Starting API server..."
ProductData__FilePath="$PRODUCT_FILE_ABS" \
EnrichmentEndpoint__MaxRequestSizeBytes="$MAX_SIZE" \
dotnet run --project src/Api/Api.csproj &
API_PID=$!

# Cleanup on exit
cleanup() {
    echo ""
    echo "Stopping API server (PID: $API_PID)..."
    kill $API_PID 2>/dev/null || true
    wait $API_PID 2>/dev/null || true
}
trap cleanup EXIT

# Wait for server to be ready
echo "Waiting for server to start..."
for i in {1..30}; do
    if curl -s "http://localhost:$PORT/api/v1/enrich" -X OPTIONS >/dev/null 2>&1; then
        echo "Server ready!"
        break
    fi
    if [[ $i -eq 30 ]]; then
        echo "Error: Server failed to start within 30 seconds"
        exit 1
    fi
    sleep 1
done

# Run the test
echo ""
echo "Sending trade file for enrichment..."
echo "File size: $(ls -lh "$TRADE_FILE_ABS" | awk '{print $5}')"
echo ""

START_TIME=$(date +%s)

HTTP_CODE=$(curl -s -o "$OUTPUT_FILE" -w "%{http_code}" \
    --max-time 600 \
    -X POST "http://localhost:$PORT/api/v1/enrich" \
    -H "Content-Type: text/csv" \
    --data-binary @"$TRADE_FILE_ABS")

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

echo "=== Results ==="
echo "HTTP Status: $HTTP_CODE"
echo "Duration:    ${DURATION}s"

if [[ "$HTTP_CODE" == "200" ]]; then
    echo "Output file: $OUTPUT_FILE"
    echo "Output size: $(ls -lh "$OUTPUT_FILE" | awk '{print $5}')"
    echo "Output rows: $(wc -l < "$OUTPUT_FILE")"
    echo ""
    echo "First 5 rows:"
    head -5 "$OUTPUT_FILE"
else
    echo "Error: Request failed"
    cat "$OUTPUT_FILE"
fi
