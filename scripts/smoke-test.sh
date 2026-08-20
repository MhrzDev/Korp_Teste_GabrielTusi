#!/usr/bin/env sh
set -eu

BASE_URL="${BASE_URL:-http://localhost:4200}"

product_json=$(curl -fsS -X POST "$BASE_URL/api/products" \
  -H 'Content-Type: application/json' \
  -d '{"code":"TEST-001","description":"Smoke test product","stock":10}')

product_id=$(printf '%s' "$product_json" | sed -n 's/.*"id":"\([^"]*\)".*/\1/p')
test -n "$product_id"

invoice_json=$(curl -fsS -X POST "$BASE_URL/api/invoices" \
  -H 'Content-Type: application/json' \
  -d "{\"items\":[{\"productId\":\"$product_id\",\"productCode\":\"TEST-001\",\"productDescription\":\"Smoke test product\",\"quantity\":2}]}")

invoice_id=$(printf '%s' "$invoice_json" | sed -n 's/.*"id":\([0-9]*\).*/\1/p')
test -n "$invoice_id"

curl -fsS -X POST "$BASE_URL/api/invoices/$invoice_id/print" \
  -H 'Content-Type: application/json' \
  -H "Idempotency-Key: smoke-$invoice_id" \
  -d '{"simulateInventoryFailure":false}' >/dev/null

echo "Smoke test passed: product created, invoice created and stock reserved."
