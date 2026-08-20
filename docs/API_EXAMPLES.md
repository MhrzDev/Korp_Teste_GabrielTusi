# API examples

Base URLs when running Docker Compose:

- Inventory: `http://localhost:8081`
- Billing: `http://localhost:8082`

## Create a product

```http
POST /api/products
Content-Type: application/json

{
  "code": "NOTE-001",
  "description": "Notebook Pro 14",
  "stock": 10
}
```

## Create an invoice

```http
POST /api/invoices
Content-Type: application/json

{
  "items": [
    {
      "productId": "PRODUCT_GUID",
      "productCode": "NOTE-001",
      "productDescription": "Notebook Pro 14",
      "quantity": 2
    }
  ]
}
```

## Print and close an invoice

```http
POST /api/invoices/1/print
Content-Type: application/json
Idempotency-Key: print-1-demo

{
  "simulateInventoryFailure": false
}
```

## Simulate Inventory failure

Use the same print endpoint with `simulateInventoryFailure: true`. The response is HTTP 503, the invoice remains Open and stock is unchanged.

