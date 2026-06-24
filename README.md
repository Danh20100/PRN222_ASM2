# PRN222 Assignment 2

## Giới thiệu
Dự án được xây dựng theo mô hình N-Tier Architecture, phục vụ cho môn học PRN222.
Ứng dụng là một trang web được xây dựng bằng ASP.NET Core Razor Pages, Entity Framework Core và SQL Server.

## Cấu trúc dự án
- **DataAccessLayer**: Chứa các Entity, DbContext và các logic giao tiếp trực tiếp với cơ sở dữ liệu.
- **BusinessLayer**: Chứa các DTO, Service xử lý logic nghiệp vụ và giao tiếp giữa DataAccessLayer và Presentation Layer.
- **PRN222_Assignment2**: Presentation Layer (Web Application) sử dụng ASP.NET Core Razor Pages.

## Công nghệ sử dụng
- **.NET 8.0**
- **ASP.NET Core Razor Pages**
- **Entity Framework Core 8.0** (Code-first / Database-first)

## Hướng dẫn cài đặt và chạy
1. Đảm bảo đã cài đặt [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
2. Clone repository về máy.
3. Mở Terminal (hoặc Command Prompt) tại thư mục gốc của project (nơi chứa file `.sln`).
4. Khôi phục các package (Restore):
   ```bash
   dotnet restore
   ```
5. Build ứng dụng:
   ```bash
   dotnet build
   ```
6. Chạy ứng dụng:
   ```bash
   cd PRN222_Assignment2
   dotnet run
   ```
7. Mở trình duyệt và truy cập vào đường dẫn hiển thị trên terminal (ví dụ: `http://localhost:5000`).

## Các cập nhật gần đây
- Cập nhật thư viện bảo mật `Azure.Identity` và `System.Text.Json` lên phiên bản mới nhất.
- Đã thêm `.gitignore` chuẩn cho Visual Studio để loại bỏ các thư mục `bin/` và `obj/` không cần thiết.
- Khắc phục lỗi build `CS1998` và `Conflicting assets`.
