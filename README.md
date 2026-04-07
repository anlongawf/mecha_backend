# Mecha Backend

Mecha is a robust backend system designed for managing user profiles, custom styles, shop interactions, and social features.

## Technologies
- **Framework**: .NET 9.0 (ASP.NET Core)
- **Database**: MariaDB / MySQL
- **ORM**: Raw SQL via `SqlConnectionHelper`
- **Authentication**: JWT & Cookie-based, with Discord OAuth support.

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- MariaDB / MySQL server

### Configuration
Update `appsettings.json` with your database connection string and JWT settings:
```json
{
  "ConnectionStrings": {
    "MariaDb": "Server=localhost;Database=mecha;Uid=root;Pwd=...;"
  },
  "Jwt": {
    "Key": "your_very_secure_key",
    "Issuer": "Mecha",
    "Audience": "MechaUsers"
  }
}
```

### Installation
1. Clone the repository.
2. Run the application:
   ```bash
   dotnet run
   ```
3. The database will be automatically initialized on the first run.

---

# API Documentation

## Base URL
`http://localhost:5000/api` (Development)

## Authentication

### 1. JWT Authentication
Most endpoints require a JWT token in the `Authorization` header:
- Header: `Authorization: Bearer <your_token>`

### 2. Cookie Authentication
The application also uses cookie-based authentication (`MechaAuth` cookie).

---

## Auth API (`/api/Auth`)

### Register
- **Endpoint**: `POST /api/auth/register`
- **Body**:
  ```json
  {
    "username": "string",
    "email": "string",
    "phone": "string",
    "password": "string"
  }
  ```
- **Description**: Registers a new user and creates a default style.

### Login
- **Endpoint**: `POST /api/auth/login`
- **Body**:
  ```json
  {
    "username": "string",
    "password": "string"
  }
  ```
- **Response**: Returns a JWT token and user info.

---

## Account API (`/api/Account`)

### Get Account Info
- **Endpoint**: `GET /api/account/{userId}`
- **Description**: Retrieves full account details including purchases, effects, and wallet balance.

### Update Account
- **Endpoint**: `PUT /api/account/{userId}`
- **Body**:
  ```json
  {
    "email": "string",
    "phone": "string"
  }
  ```

### Purchase History
- **Endpoint**: `GET /api/account/{userId}/purchases`

### Wallet / Add Coins
- **Endpoint**: `GET /api/account/{userId}/wallet`
- **Endpoint**: `POST /api/account/{userId}/wallet/add-coins`
- **Body**: `{ "amount": number }`

---

## Shop API (`/api/Shop`)

### Get Products
- **Endpoint**: `GET /api/shop/products?category=...&userId=...`
- **Description**: List available products (effects, etc.) with ownership status.

### Purchase Product
- **Endpoint**: `POST /api/shop/purchase`
- **Body**:
  ```json
  {
    "userId": number,
    "productId": number,
    "paymentMethod": "string"
  }
  ```

### User Effects
- **Endpoint**: `GET /api/shop/user/{userId}/effects`
- **Endpoint**: `PUT /api/shop/user/{userId}/effect/{effectId}/apply`
- **Endpoint**: `DELETE /api/shop/user/{userId}/effect/{effectId}` (Deactivates effect)

---

## Profile API (`/api/Profile`)

### Get Profile
- **Endpoint**: `GET /api/profile/{id}`
- **Endpoint**: `GET /api/profile/username/{username}`

### Update Profile
- **Endpoint**: `POST /api/profile/{id}` or `PUT /api/profile/{id}`
- **Body**: `UpdateProfileDto`

### Change Username
- **Endpoint**: `PUT /api/profile/{id}/change-username`
- **Body**: `"new_username"` (string)

---

## Styles API (`/api/Styles`)

### Manage Styles
- **GET**: `/api/styles` - List all styles
- **GET**: `/api/styles/{id}` - Get specific style
- **POST**: `/api/styles` - Create new style
- **PUT**: `/api/styles/{id}` - Update style
- **DELETE**: `/api/styles/{id}` - Delete style

---

## User Styles API (`/api/UserStyles`)

- **GET**: `/api/UserStyles/username/{username}`
- **GET**: `/api/UserStyles/{idUser}`
- **POST**: `/api/UserStyles`
- **PUT**: `/api/UserStyles/{idUser}`
- **DELETE**: `/api/UserStyles/{idUser}`

---

## File Upload API (`/api/FileUpload`)

### Upload File
- **Endpoint**: `POST /api/FileUpload/upload?type=...&userId=...`
- **Form Data**: `file` (IFormFile)
- **Types**: `image`, `background_image`, `background_video` (Premium only), `audio`, `audio_image`.

### Delete File
- **Endpoint**: `DELETE /api/FileUpload/delete`
- **Body**: `{ "path": "/uploads/..." }`

### Check Premium
- **Endpoint**: `GET /api/FileUpload/check-premium/{userId}`

---

## Data Models (DTOs)

### ProductDto
```json
{
  "productId": number,
  "name": "string",
  "description": "string",
  "price": number,
  "premiumOnly": boolean,
  "type": "effect",
  "category": "string",
  "icon": "string",
  "previewImage": "string",
  "isOwned": boolean,
  "isApplied": boolean
}
```

### WalletDto
```json
{
  "walletId": number,
  "userId": number,
  "coins": number,
  "lastUpdated": "datetime"
}
```
