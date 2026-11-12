# GarageHub

Ứng dụng web đặt lịch sửa xe tại garage (dự án môn học, team 5 người). Người dùng đăng nhập bằng Google, đặt lịch theo ca, thanh toán đặt cọc online qua PayOS, quét biển số (OCR) để tự điền thông tin xe. App còn lưu lịch sử sửa chữa theo xe, đánh giá garage và nhắc bảo dưỡng định kỳ.

File này là bản tóm tắt những gì team ĐÃ CHỐT. Nếu yêu cầu của người dùng mâu thuẫn với file này, hãy hỏi lại trước khi làm.

---

## 1. RÀNG BUỘC KHI LÀM VIỆC (đọc kỹ, ưu tiên cao nhất)

### Thiếu thông tin thì HỎI, không đoán
- Nếu để làm đúng task cần một quyết định hoặc thông tin chưa có trong file này hay trong yêu cầu, DỪNG và hỏi người dùng trước khi viết code. Không tự chọn giá trị tạm, không tự giả định.
- Các điểm TEAM CHƯA CHỐT (luôn phải hỏi, không tự quyết):
  - Công nghệ OCR đọc biển số (`IOcrService` hiện chỉ là placeholder).
  - Booking chuyển `Pending` → `Confirmed` tự động khi cọc thành công, hay garage xác nhận thủ công.
  - Cách gán role `GarageOwner` và `Admin`.
  - Quy tắc hoàn cọc khi hủy booking và cách tính số tiền cọc.
  - Giao diện/wireframe (team dùng giao diện tham khảo từ web khác, chưa có thiết kế riêng).
- Câu hỏi phải cụ thể: nêu rõ cần quyết định gì và các lựa chọn, không hỏi chung chung.

### Chỉ làm đúng phần được giao
- Làm đúng phạm vi task người dùng nêu, không làm thêm "tiện tay", không mở rộng tính năng, không tự thêm module hay file ngoài yêu cầu.
- Không implement phần thuộc người/module khác (ví dụ đang làm Booking thì không viết Payment, OAuth, OCR).
- Không tự tạo interface/class của module khác. Nếu task cần một interface chưa có, hỏi người dùng.

### Không sửa phần không liên quan
- Không sửa, đổi tên, format lại, refactor hay xoá code/file nằm ngoài phạm vi task. Diff phải nhỏ nhất có thể.
- Không đổi config chung (`Program.cs` ngoài phần đăng ký cần thiết cho task, `appsettings.json`, `.gitignore`, `.csproj`), không nâng version hay thêm NuGet package nếu không được yêu cầu.
- Nếu muốn chạy được task buộc phải sửa phần ngoài phạm vi: DỪNG, nói rõ file nào, sửa gì, vì sao, rồi chờ người dùng đồng ý.

### Database và migration
- Không tự đổi entity, cột, index, ràng buộc hay enum đã chốt ở mục 4.
- Không sửa hoặc xoá migration đã có trên nhánh `dev`/`main`. Thay đổi schema = migration MỚI, và chỉ làm khi người dùng yêu cầu rõ.

### Git và bảo mật
- Không commit, push, tạo/đổi nhánh hay merge trừ khi người dùng yêu cầu. Không push trực tiếp vào `main`/`dev`.
- KHÔNG thêm dòng `Co-Authored-By` hay bất kỳ attribution nào của Claude vào commit message hoặc mô tả PR. Người dùng là người đóng góp duy nhất.
- Không commit secret (connection string thật, Google OAuth client secret, PayOS API key). Secret dùng `dotnet user-secrets`; `appsettings.json` chỉ chứa placeholder.

### Báo cáo khi xong
- Chạy build thành công trước khi báo xong.
- Báo cáo ngắn gọn: file đã tạo/sửa, điều chưa rõ hoặc đã hỏi, và phần cố ý KHÔNG làm.

---

## 2. Tech stack

- ASP.NET Core 10, MVC (Razor Views), kiến trúc 3-layer
- SQL Server, EF Core Code First
- Đăng nhập: Google OAuth + cookie authentication (KHÔNG dùng ASP.NET Core Identity)
- Thanh toán: PayOS
- OCR: chưa chốt

## 3. Kiến trúc

```text
Kiến trúc (3-layer, MVC)
├── Luồng gọi: Web (Controller + View) → Service → Repository, giao tiếp qua interface
├── Interface theo module
│   ├── IUserService / IUserRepository
│   ├── IVehicleService / IVehicleRepository
│   ├── IGarageService / IGarageRepository
│   ├── IBookingService / IBookingRepository
│   ├── IPaymentService / IPaymentRepository (+ IPayOSClient)
│   ├── IReviewService / IReviewRepository
│   ├── IReminderService / IReminderRepository
│   └── IOcrService (placeholder, chờ chốt OCR)
├── DTO: đặt ở GarageHub.Common, không đưa Entity ra View
├── API riêng ngoài MVC
│   ├── POST /api/payments/payos-callback (webhook PayOS)
│   └── POST /api/ocr/scan-plate (AJAX OCR)
├── Auth (không dùng Identity)
│   ├── Cookie authentication + đăng nhập Google
│   ├── Bảng Users tự quản: lần đầu đăng nhập tạo dòng Users, Role mặc định Customer
│   └── Role (Customer / GarageOwner / Admin) đưa vào cookie
├── Validate: Data Annotations
└── Xử lý lỗi: middleware tập trung
```

Cấu trúc solution:

```text
GarageHub/
├── GarageHub.sln
├── .gitignore
├── README.md
├── src/
│   ├── GarageHub.Web/          # MVC: Controllers, Views, wwwroot
│   ├── GarageHub.Service/      # Business logic
│   ├── GarageHub.Repository/   # EF Core DbContext, Repository, Entities
│   └── GarageHub.Common/       # DTOs, constants, helper dùng chung
└── tests/
    └── GarageHub.Tests/        # Unit test
```

## 4. Database (đã chốt)

Mọi khoá ngoại dùng `DeleteBehavior.Restrict` (tránh lỗi multiple cascade paths của SQL Server, giữ lịch sử sửa chữa). Enum lưu dạng chuỗi.

| Bảng | Cột | Ghi chú |
|---|---|---|
| Users | Id (PK), FullName, Email, GoogleId (nullable), Role, CreatedAt | Role: Customer / GarageOwner / Admin |
| Vehicles | Id (PK), OwnerId (FK → Users), PlateNumber, Brand, Model, CreatedAt | PlateNumber unique toàn hệ thống |
| Garages | Id (PK), OwnerId (FK → Users, nullable), Name, Address, Phone, AvgRating, CapacityPerSlot | Mỗi garage tối đa 1 tài khoản chủ; seed mẫu CapacityPerSlot = 2 |
| ServiceBookings | Id (PK), VehicleId (FK), GarageId (FK), Status, ScheduledAt, CreatedAt, ExpiresAt (nullable) | Index (GarageId, ScheduledAt); ScheduledAt = giờ bắt đầu của ca; ExpiresAt = hạn giữ chỗ của booking Pending |
| Payments | Id (PK), BookingId (FK), Amount, Status, PayOSTransactionId (nullable), PaidAt (nullable) | 1 booking có thể có nhiều Payment (thanh toán lại khi link hết hạn) |
| Reviews | Id (PK), BookingId (FK), Rating, Comment (nullable), CreatedAt | Unique index trên BookingId (mỗi booking tối đa 1 review) |
| Reminders | Id (PK), VehicleId (FK), DueDate, Type, Status | Không có CreatedAt |

Enum:
- `BookingStatus`: Pending / Confirmed / InProgress / Completed / Canceled
- `PaymentStatus`: Pending / Confirmed / Canceled
- `ReminderStatus`: Pending / Due / Done
- Chính tả là `Canceled` (một chữ L).

Quan hệ: Users 1-n Vehicles; Users 0..1-n Garages (qua OwnerId); Vehicles 1-n ServiceBookings; Garages 1-n ServiceBookings; ServiceBookings 1-n Payments; ServiceBookings 1-0..1 Reviews; Vehicles 1-n Reminders.

## 5. Quy tắc nghiệp vụ đã chốt: đặt lịch theo ca

- Mỗi ngày có 4 ca cố định, mỗi ca 2 tiếng, dùng chung cho mọi garage: 08:00–10:00, 10:00–12:00, 13:00–15:00, 15:00–17:00. Người dùng chọn ca, không nhập giờ tự do.
- Mỗi garage nhận tối đa `CapacityPerSlot` booking mỗi ca (tối đa 4 × CapacityPerSlot mỗi ngày).
- Khi đếm, tính các booking có trạng thái Pending + Confirmed + InProgress (không tính Canceled) cùng `GarageId` và cùng `ScheduledAt`.
- Booking `Pending` giữ chỗ 15 phút (`ExpiresAt`). Hết hạn mà chưa thanh toán cọc thì nhả chỗ.
- Cần xử lý race condition khi 2 người đặt chỗ cuối cùng cùng lúc (dùng transaction).
- Booking là giữ chỗ cho giờ xe đến, không mô tả thời gian sửa.

## 6. Quy ước thời gian

- TẤT CẢ cột thời gian (ScheduledAt, CreatedAt, ExpiresAt, PaidAt, DueDate) lưu GIỜ VIỆT NAM (UTC+7, không có giờ mùa hè).
- `CreatedAt` có default ở DB là `DATEADD(HOUR, 7, GETUTCDATE())` để không phụ thuộc múi giờ máy chủ.
- Trong code chỉ lấy giờ hiện tại qua một helper dùng chung ở `GarageHub.Common` (UTC now + 7 giờ). Không dùng `DateTime.Now` / `DateTime.UtcNow` trực tiếp.
- Nếu dữ liệu từ bên ngoài (ví dụ PayOS) trả về UTC hoặc epoch, đổi sang giờ Việt Nam trước khi lưu.

## 7. Quy ước code và git

- Naming: class/method/property → PascalCase; biến local/param → camelCase; field private → `_camelCase`; interface có tiền tố `I`.
- Nhánh: `main` (luôn chạy được) + `dev` (tích hợp) + `feature/<module>-<việc>`. Merge vào `dev` qua PR, cần ít nhất 1 người approve.
- Commit message: `<loại>: <mô tả ngắn>`, loại gồm feat / fix / refactor / docs / test / chore.
