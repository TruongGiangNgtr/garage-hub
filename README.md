# GarageHub
## Giới thiệu

GarageHub là ứng dụng web giúp người dùng đặt lịch sửa xe tại các garage gần nơi họ ở. Người dùng đăng nhập qua Google OAuth, đặt lịch sửa xe, thanh toán đặt cọc trực tuyến và xem lịch sử sửa chữa. Tính năng OCR mới chỉ có nút giao diện mẫu, chưa xử lý ảnh.

## Local configuration

The web project uses Google OAuth for sign-in and profile completion. In Google Cloud Console, register this authorized redirect URI for the HTTPS launch profile:

```text
https://localhost:7100/signin-google
```

Set credentials and the payment callback base URL in user secrets (do not commit these values):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<SQL Server connection string>" --project src\GarageHub.Web
dotnet user-secrets set "GOOGLE_CLIENT_ID" "<Google OAuth client ID>" --project src\GarageHub.Web
dotnet user-secrets set "GOOGLE_CLIENT_SECRET" "<Google OAuth client secret>" --project src\GarageHub.Web
dotnet user-secrets set "APP_PUBLIC_BASE_URL" "<public HTTPS base URL>" --project src\GarageHub.Web
dotnet user-secrets set "PAYOS_CLIENT_ID" "<PayOS client ID>" --project src\GarageHub.Web
dotnet user-secrets set "PAYOS_API_KEY" "<PayOS API key>" --project src\GarageHub.Web
dotnet user-secrets set "PAYOS_CHECKSUM_KEY" "<PayOS checksum key>" --project src\GarageHub.Web
```

The database connection string and PayOS credentials must also be configured in user secrets. The PayOS webhook endpoint is `POST /api/payments/payos-callback`. A booking creates a 15-minute pending hold and a 50,000 VND payment; a verified successful PayOS payment confirms the booking. Google accounts receive the Customer role by default. Garage management is limited to GarageOwner and Admin accounts; role assignment is not part of this implementation. Vehicle scanning is currently a UI placeholder and does not upload or process the selected file.

## SQL Server database

Open [GarageHub.Database.sql](./GarageHub.Database.sql) in SQL Server Management Studio and execute it while connected to `localhost`. The script creates the `GarageHub` database if it does not exist, then applies the EF Core initial schema and sample garages. It does not drop an existing database. The SQL account needs permission to create the database and tables.

For SQL Server username/password authentication, set the connection string in user secrets. Replace the placeholders locally; do not commit real credentials:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=GarageHub;User Id=<SQL_USERNAME>;Password=<SQL_PASSWORD>;Encrypt=True;TrustServerCertificate=True;" --project src\GarageHub.Web
```

For local development, `TrustServerCertificate=True` avoids certificate trust errors. Use a valid SQL Server certificate instead in production.
