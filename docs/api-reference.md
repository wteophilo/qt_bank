# API Endpoint Reference

All protected endpoints require a `Authorization: Bearer <token>` header.

## 🔑 Authentication

### `POST /api/v1/auth/token`
Generates a JWT bearer token for the specified user (useful for testing).
- **Request Body**:
  ```json
  {
    "Username": "Alice Smith"
  }
  ```
- **Response (`200 OK`)**:
  ```json
  {
    "token": "eyJhbGciOiJIUzI1Ni..."
  }
  ```
- **Failure Response (`400 Bad Request` - Validation Error)**:
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "Validation Error",
    "status": 400,
    "detail": "One or more validation errors occurred.",
    "errors": {
      "Username": [
        "'Username' must not be empty."
      ]
    }
  }
  ```

---

## 👤 Accounts

### `GET /api/v1/accounts/{accountNumber}/balance`
Retrieves the balance for a specific account.
- **Response (`200 OK`)**:
  ```json
  {
    "accountNumber": "111111",
    "ownerName": "Alice Smith",
    "balance": 5000.00,
    "currency": "USD"
  }
  ```
- **Failure Response (`404 Not Found` - Account Not Found)**:
  ```json
  {
    "error": "Account with number '999999' not found."
  }
  ```

### `GET /api/v1/accounts/{accountNumber}/transactions`
Retrieves transaction history for a specific account, sorted by creation date in descending order (`CreatedAt` desc) so that the newest transactions appear first.
- **Response (`200 OK`)**:
  ```json
  [
    {
      "id": "713c7bb6-bf25-4c07-ba71-fa2b8493d05e",
      "sourceAccountNumber": "111111",
      "destinationAccountNumber": "222222",
      "amount": 49.50,
      "currency": "USD",
      "type": "Transfer",
      "status": "Completed",
      "createdAt": "2026-06-23T22:26:51Z"
    }
  ]
  ```
- **Failure Response (`404 Not Found` - Account Not Found)**:
  ```json
  {
    "error": "Account with number '999999' not found."
  }
  ```

---

## 💸 Transactions

### `POST /api/v1/transactions/deposit`
Deposits money into an account.
- **Request Body**:
  ```json
  {
    "AccountNumber": "111111",
    "Amount": 500.00,
    "Currency": "USD",
    "IdempotencyKey": "5fa85f64-5717-4562-b3fc-2c963f66afa6"
  }
  ```
- **Response (`202 Accepted`)**:
  ```json
  {
    "transactionId": "5c907b22-a72a-4310-a4a3-76f5de0e44f5",
    "status": "Completed",
    "createdAt": "2026-06-28T16:21:18Z"
  }
  ```
- **Failure Response (`400 Bad Request` - Account Inactive)**:
  ```json
  {
    "error": "Account is not active."
  }
  ```
- **Failure Response (`400 Bad Request` - Validation Error)**:
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "Validation Error",
    "status": 400,
    "detail": "One or more validation errors occurred.",
    "errors": {
      "Amount": [
        "Amount must be greater than zero."
      ]
    }
  }
  ```

### `POST /api/v1/transactions/withdrawal`
Withdraws money from an account.
- **Request Body**:
  ```json
  {
    "AccountNumber": "111111",
    "Amount": 100.00,
    "Currency": "USD",
    "IdempotencyKey": "b2c63c78-cf52-4f36-a11c-d784a0d9e8b3"
  }
  ```
- **Response (`202 Accepted`)**:
  ```json
  {
    "transactionId": "abcf89b3-c12f-410a-ba92-dcd12f451f28",
    "status": "Completed",
    "createdAt": "2026-06-28T16:21:18Z"
  }
  ```
- **Failure Response (`400 Bad Request` - Insufficient Funds)**:
  ```json
  {
    "error": "Insufficient funds."
  }
  ```

### `POST /api/v1/transactions/transfer`
Transfers money from a source account to a destination account.
- **Request Body**:
  ```json
  {
    "SourceAccountNumber": "111111",
    "DestinationAccountNumber": "222222",
    "Amount": 250.00,
    "Currency": "USD",
    "IdempotencyKey": "a9a3b98c-2fbb-49e0-82fa-25f0a6d5952f"
  }
  ```
- **Response (`202 Accepted`)**:
  ```json
  {
    "transactionId": "f78ab30b-5b32-4752-b1cf-712803b9e4a3",
    "status": "Completed",
    "createdAt": "2026-06-28T16:21:18Z"
  }
  ```
- **Failure Response (`400 Bad Request` - Same Source and Destination)**:
  ```json
  {
    "error": "Source and destination accounts must be different."
  }
  ```

---

## 🗄️ Pre-seeded Testing Data

To make testing in **Swagger UI** immediate, the in-memory repository is populated on application startup with the following mock entries:

### Mock Accounts

1. **Alice Smith** (Active)
   - **ID**: `11111111-1111-1111-1111-111111111111`
   - **Account Number**: `111111`
   - **Balance**: `$5,000.00`
   - **Status**: `Active`
2. **Bob Johnson** (Active)
   - **ID**: `22222222-2222-2222-2222-222222222222`
   - **Account Number**: `222222`
   - **Balance**: `$150.50`
   - **Status**: `Active`
3. **Charlie Davis** (Inactive)
   - **ID**: `33333333-3333-3333-3333-333333333333`
   - **Account Number**: `333333`
   - **Balance**: `$0.00`
   - **Status**: `Inactive`