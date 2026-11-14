# GarageHub
## Giới thiệu

GarageHub là ứng dụng web giúp người dùng đặt lịch sửa xe tại các garage gần nơi họ ở. Người dùng đăng nhập qua Google/Facebook (OAuth), đặt lịch sửa xe, thanh toán đặt cọc trực tuyến, và sử dụng tính năng quét biển số (OCR) để tự động điền thông tin xe khi đăng ký lịch hẹn. Ngoài ra, app lưu lịch sử sửa chữa theo từng xe, cho phép đánh giá garage, và nhắc bảo dưỡng định kỳ.

## Cài đặt và chạy trên máy

### Yêu cầu
- .NET SDK 10
- SQL Server (bản Developer/Express đều được)
- Công cụ EF Core: `dotnet tool install --global dotnet-ef`

### 1. Lấy key Google OAuth
Repo **không** chứa secret. Nhắn riêng người phụ trách Auth để:
- Lấy **Client ID** và **Client secret** (gửi qua tin nhắn riêng, **không** commit, không dán vào group công khai).
- Nhờ thêm Gmail của bạn vào **Test users** trên Google Cloud. Chưa được thêm thì Google sẽ báo "Access blocked".

### 2. Đặt user-secrets
Chạy tại thư mục gốc của repo. Secret được lưu trên máy bạn, không nằm trong git.

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>" --project src/GarageHub.Web
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>" --project src/GarageHub.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=<ten-sql-server>;Database=GarageHub;Trusted_Connection=True;TrustServerCertificate=True" --project src/GarageHub.Web
```

- `<ten-sql-server>` tùy máy: `localhost`, `.\SQLEXPRESS`, `localhost\SQL2025`, `(localdb)\MSSQLLocalDB`...
- Muốn tài khoản của mình là **Admin** (vào được trang `/Admin/Users`):
  ```bash
  dotnet user-secrets set "Auth:AdminEmails:0" "<gmail-cua-ban>" --project src/GarageHub.Web
  ```
- Kiểm tra lại: `dotnet user-secrets list --project src/GarageHub.Web`

### 3. Tạo database và chạy app
```bash
dotnet ef database update -p src/GarageHub.Repository -s src/GarageHub.Web
dotnet dev-certs https --trust
dotnet run --project src/GarageHub.Web --launch-profile https
```

Mở **https://localhost:7100** → **Đăng nhập** → **Đăng nhập bằng Google**.

> Phải chạy bản **https** (cổng 7100). Google chỉ chấp nhận Redirect URI `https://localhost:7100/signin-google`.
>
> Mỗi lần pull về mà có migration mới thì chạy lại `dotnet ef database update`.

### Lỗi thường gặp
| Lỗi | Cách sửa |
|---|---|
| Trang Login báo "Chưa cấu hình Google OAuth" | Thiếu hoặc sai tên key ở bước 2 |
| `Access blocked` / `access_denied` | Gmail chưa có trong Test users, nhờ người phụ trách Auth thêm |
| `redirect_uri_mismatch` | Đang không chạy ở `https://localhost:7100` |
| `Correlation failed` | Đang chạy bản http, chạy lại với `--launch-profile https` |
| Không kết nối được database | Sai tên SQL Server trong connection string, hoặc SQL Server chưa chạy |

## Ghi chú cho người làm module khác

- Người dùng đăng nhập lần đầu được tạo trong bảng `Users` với role `Customer`. Email nằm trong `Auth:AdminEmails` được thành `Admin`. Admin đổi role người khác ở `/Admin/Users`, có hiệu lực ngay không cần đăng nhập lại.
- Trang cần đăng nhập: gắn `[Authorize]` lên controller hoặc action.
- Lấy Id người đang đăng nhập (là `Users.Id` trong DB, không phải Google Id):
  ```csharp
  using GarageHub.Web.Extensions;

  var userId = User.GetUserId();     // int
  var role = User.GetUserRole();     // UserRole?
  ```
- Phân quyền theo role:
  ```csharp
  using GarageHub.Web.Auth;

  [Authorize(Policy = AuthPolicies.RequireAdmin)]
  [Authorize(Policy = AuthPolicies.RequireGarageOwner)]
  ```
- Ở tầng Service dùng `IUserService` (`GetByIdAsync`, `GetRoleAsync`...), không truy cập bảng `Users` trực tiếp.
