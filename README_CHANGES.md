# Báo Cáo Thay Đổi Tầng Backend (BE Changes Changelog)

Tài liệu này tổng hợp toàn bộ các thay đổi và cải tiến mới nhất được triển khai trong dự án backend **InteriorAI** (ASP.NET Core & EF Core).

---

## 🛠️ Danh Sách Các File Thay Đổi & Vai Trò

Dưới đây là chi tiết các file mới và file được chỉnh sửa, được phân loại theo từng thành phần kiến trúc:

| Thành phần | Trạng thái | Đường dẫn File | Mô tả chi tiết thay đổi |
| :--- | :--- | :--- | :--- |
| **Configuration** | **[MODIFY]** | [appsettings.json](file:///d:/nhom5/BE/InteriorAI/appsettings.json) | Cập nhật `DefaultConnection` chuyển từ server tên máy `DESKTOP-FJL7B4L` sang `.` (dấu chấm) giúp tăng tốc và tránh lỗi kết nối SQL Server (Error 40 - Named Pipes). |
| **DTO (Data Transfer Object)** | **[NEW]** | [UploadAvatarRequest.cs](file:///d:/nhom5/BE/InteriorAI/Models/DTOs/UploadAvatarRequest.cs) | Khai báo DTO đóng gói `UserId` và `File` (`IFormFile`) cho luồng tải ảnh đại diện lên hệ thống. |
| **Controller** | **[MODIFY]** | [AuthController.cs](file:///d:/nhom5/BE/InteriorAI/Controllers/AuthController.cs) | Chuyển đổi tham số hàm `UploadAvatar` sang DTO `UploadAvatarRequest` để hỗ trợ cơ chế Model Binding mạnh mẽ, tránh lỗi binding dữ liệu từ client Android. |
| **Entity (Database Model)** | **[MODIFY]** | [DesignResult.cs](file:///d:/nhom5/BE/InteriorAI/Models/Entities/DesignResult.cs) | Khai báo thêm thuộc tính `IsDeleted` (`bool`, mặc định là `false`) để kích hoạt tính năng **Xóa Mềm (Soft Delete)** cho lịch sử dự án. |
| **Migrations** | **[NEW]** | [20260523074111_AddIsDeletedToDesignResult.cs](file:///d:/nhom5/BE/InteriorAI/Migrations/20260523074111_AddIsDeletedToDesignResult.cs) | Migration của Entity Framework Core để đồng bộ hóa cột `IsDeleted` vào bảng `DesignResults` trong SQL Server. |
| **Service (Business Layer)** | **[MODIFY]** | [DesignManager.cs](file:///d:/nhom5/BE/InteriorAI/Services/DesignManager.cs) | Triển khai logic nghiệp vụ: truy vấn lịch sử dự án phân trang (có lọc bản ghi đã xóa) và thực hiện xóa mềm thực thể. |
| **Controller (API Endpoints)** | **[MODIFY]** | [DesignController.cs](file:///d:/nhom5/BE/InteriorAI/Controllers/DesignController.cs) | Định nghĩa các đầu API RESTful mới cho truy vấn lịch sử, xóa mềm dự án, đồng thời tối ưu hóa Swagger UI bằng cách hứng header `user-id` trực tiếp qua `[FromHeader]`. |

---

## 🔍 Chi Tiết Các Cải Tiến Lớn

### 1. Cấu hình Chuỗi Kết Nối CSDL Siêu Bền (appsettings.json)
* **Vấn đề cũ:** Máy chủ database được chỉ định dưới dạng NetBIOS hostname `DESKTOP-FJL7B4L`. Lựa chọn này dễ gặp trục trặc khi mạng local phân giải tên chậm hoặc khi tường lửa máy tính chặn cổng, dẫn đến lỗi crash app khi khởi động (`SqlException 0x80131904`).
* **Giải pháp mới:** Đổi sang địa chỉ server rút gọn `.` (dấu chấm). Đây là định danh chuẩn cho Local SQL Server Default Instance, giúp kết nối trực tiếp qua giao thức Shared Memory nội bộ cực nhanh và tránh mọi xung đột mạng/DNS.

### 2. Sửa Lỗi Tải Ảnh Đại Diện Cho Android Client (AuthController.cs)
* **Thay đổi:** Nâng cấp phương thức endpoint `UploadAvatar`:
  ```csharp
  [HttpPost("upload-avatar")]
  public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarRequest request)
  ```
* **Lợi ích:** Đóng gói toàn bộ payload vào DTO `UploadAvatarRequest` giúp ASP.NET Core tự động giải mã kiểu dữ liệu dạng `multipart/form-data` chính xác nhất. Giải quyết triệt để lỗi mất dữ liệu trường `userId` hoặc mất luồng file nhị phân `IFormFile` khi gọi API từ ứng dụng Android hoặc Retrofit.

### 3. Tính Năng Xem Lịch Sử Thiết Kế Phân Trang (DesignManager.cs & DesignController.cs)
* **API mới:** `GET /api/design/projects`
* **Tham số:** 
  * Header: `user-id` (Mã định danh của người dùng)
  * Query: `page` (Trang hiện tại, mặc định = 1), `pageSize` (Số bản ghi mỗi trang, mặc định = 10, tối đa = 50)
* **Đặc tính kỹ thuật:** 
  * Áp dụng `.AsNoTracking()` trong EF Core để tối ưu tốc độ đọc và hạn chế chiếm dụng bộ nhớ RAM máy chủ.
  * Chỉ hiển thị các thiết kế thuộc quyền sở hữu của chính user đó và chưa bị xóa (`!d.IsDeleted`).
  * Trả về siêu dữ liệu phân trang đầy đủ bao gồm: `data` (danh sách), `page` (trang), `pageSize` (kích cỡ trang), `total` (tổng số bản ghi), `totalPages` (tổng số trang).

### 4. Tính Năng Xóa Mềm Dự Án (Soft Delete)
* **API mới:** `DELETE /api/design/{designId}`
* **Luồng xử lý nghiệp vụ:**
  1. Kiểm tra xem dự án (`designId`) có tồn tại trong hệ thống hay không (trả về `404 Not Found` nếu không tìm thấy).
  2. Xác thực quyền sở hữu: Kiểm tra xem dự án đó có thuộc về user đang yêu cầu xóa hay không (trả về `403 Forbidden` nếu vi phạm).
  3. Cập nhật trạng thái `IsDeleted = true` và mốc thời gian cập nhật `UpdatedAt = DateTime.UtcNow`.
  4. Lưu thay đổi. Dự án sẽ lập tức ẩn khỏi danh sách lịch sử mà không cần phải thực hiện câu lệnh xóa cứng nguy hiểm trên database vật lý.

---

## 🚀 Hướng Dẫn Chạy & Kiểm Thử Trên Môi Trường Của Bạn

1. **Khởi động ứng dụng:**
   ```bash
   dotnet run
   ```
   *Ứng dụng sẽ hoạt động ổn định trên cổng mặc định:* `http://localhost:5207`

2. **Truy cập Swagger Playground:**
   Mở trình duyệt bất kỳ và truy cập địa chỉ: [http://localhost:5207/swagger/index.html](http://localhost:5207/swagger/index.html) để thực hiện gửi thử các request, lấy lịch sử thiết kế và chạy tính năng xóa mềm trực quan nhất.
