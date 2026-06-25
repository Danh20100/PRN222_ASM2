# 📚 RAG-LMS — Retrieval-Augmented Generation Learning Management System

> **PRN222 Assignment 2** — Hệ thống quản lý học tập tích hợp AI sử dụng kỹ thuật RAG (Retrieval-Augmented Generation) để hỗ trợ hỏi đáp tài liệu môn học.

---

## 📖 Mô tả

RAG-LMS là ứng dụng web ASP.NET Core 8 Razor Pages cho phép giáo viên upload tài liệu môn học, tự động chia nhỏ (chunking) và tạo vector embeddings, từ đó sinh viên có thể **chat hỏi đáp trực tiếp** với nội dung tài liệu thông qua AI. Hệ thống còn hỗ trợ **benchmark** để đánh giá chất lượng RAG pipeline với các cấu hình embedding/chunking khác nhau.

---

## ✨ Tính năng chính

| Module | Mô tả |
|---|---|
| **🔐 Authentication** | Đăng nhập / Đăng xuất với Cookie Authentication, phân quyền theo Role (Admin, Teacher, Student) |
| **👥 Quản lý User** | Admin quản lý danh sách người dùng |
| **📘 Quản lý Môn học (Subjects)** | CRUD môn học, gán giáo viên cho môn học, cập nhật real-time qua SignalR |
| **📄 Quản lý Tài liệu (Documents)** | Upload tài liệu, tự động chunking với nhiều chiến lược, tạo vector embeddings |
| **💬 Chat AI (RAG)** | Hỏi đáp dựa trên nội dung tài liệu, hỗ trợ trích dẫn nguồn (citations), lưu lịch sử chat |
| **📊 Benchmark** | Đánh giá chất lượng RAG pipeline, so sánh các embedding models & chunking strategies |
| **👤 Profile** | Xem và chỉnh sửa thông tin cá nhân |

---

## 🏗️ Kiến trúc

Dự án sử dụng kiến trúc **3-Layer Architecture**:

```
PRN222_Assignment2.sln
│
├── PRN222_Assignment2/        # Presentation Layer (Razor Pages)
│   ├── Pages/
│   │   ├── Admin/             # Quản lý user (Admin only)
│   │   ├── Auth/              # Login / Logout
│   │   ├── Benchmark/         # RAG benchmark
│   │   ├── Chat/              # Chat AI với tài liệu
│   │   ├── Documents/         # Upload & quản lý tài liệu
│   │   ├── Profile/           # Thông tin cá nhân
│   │   └── Subjects/          # CRUD môn học
│   ├── Hubs/                  # SignalR Hubs (real-time)
│   └── wwwroot/               # Static files (CSS, JS, images)
│
├── BusinessLayer/             # Business Logic Layer
│   ├── Services/              # AuthService, ChatService, DocumentService, ...
│   ├── Interfaces/            # IEmbeddingProvider
│   ├── Strategies/            # Chunking strategies (Strategy Pattern)
│   ├── DTOs/                  # Data Transfer Objects
│   ├── Helpers/               # SecurityHelper, VectorHelper
│   └── Models/                # Settings POCOs
│
└── DataAccessLayer/           # Data Access Layer
    ├── Context/               # AppDbContext (EF Core)
    ├── Entities/              # Domain entities
    ├── Repositories/          # Generic Repository + Unit of Work
    └── Migrations/            # EF Core Migrations
```

---

## 🛠️ Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| **Framework** | ASP.NET Core 8.0 — Razor Pages |
| **ORM** | Entity Framework Core 8.0 |
| **Database** | SQL Server (LocalDB) |
| **Authentication** | Cookie Authentication |
| **Real-time** | SignalR |
| **AI / Embedding** | Gemini API, HuggingFace, OpenAI, Ollama |
| **Design Patterns** | Repository, Unit of Work, Strategy, Factory |
| **CSV Processing** | CsvHelper |

---

## 📋 Yêu cầu hệ thống

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) trở lên
- SQL Server (LocalDB hoặc SQL Server Express)
- Visual Studio 2022 (khuyến nghị) hoặc VS Code
- **Gemini API Key** (bắt buộc)

---

## 🚀 Hướng dẫn cài đặt

### 1. Clone repository

```bash
git clone https://github.com/Danh20100/PRN222_ASM2.git
cd PRN222_ASM2
```

### 2. Cấu hình API Keys

Mở file `PRN222_Assignment2/appsettings.json` và cập nhật API keys:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RagLmsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "ApiKeys": {
    "Gemini": "<YOUR_GEMINI_API_KEY>",
    "HuggingFace": "",
    "OpenAI": "",
    "OllamaBaseUrl": "http://localhost:11434"
  }
}
```

> ⚠️ **Lưu ý:** Gemini API Key là bắt buộc. Ứng dụng sẽ throw exception nếu thiếu key này.

### 3. Chạy ứng dụng

```bash
dotnet restore
dotnet run --project PRN222_Assignment2
```

Database sẽ được **tự động migrate** khi ứng dụng khởi động.

### 4. Truy cập

Mở trình duyệt và truy cập: `https://localhost:5001` (hoặc port hiển thị trên console)

---

## 🔑 Phân quyền (Roles)

| Role | Quyền truy cập |
|---|---|
| **Admin** | Quản lý Users, Subjects, Documents, Benchmark, Chat |
| **Teacher** | Subjects, Documents, Benchmark, Chat |
| **Student** | Chat (hỏi đáp tài liệu), Profile |

---

## 🧩 Design Patterns

### Strategy Pattern — Chunking Strategies
Hệ thống hỗ trợ 4 chiến lược chia nhỏ tài liệu:
- **Fixed-Size Chunking** — Chia theo kích thước cố định
- **Paragraph Chunking** — Chia theo đoạn văn
- **Sentence Chunking** — Chia theo câu
- **Recursive Chunking** — Chia đệ quy theo nhiều dấu phân cách

### Factory Pattern — Embedding Providers
Hỗ trợ nhiều nhà cung cấp embedding:
- **Gemini** (Google AI)
- **HuggingFace**
- **OpenAI**
- **Ollama** (local)

### Repository & Unit of Work
- `GenericRepository<T>` cho các thao tác CRUD chung
- `UnitOfWork` quản lý transaction xuyên suốt nhiều repository

---

## 📁 Entity Diagram

```
User ──< SubjectTeacher >── Subject ──< Chapter ──< Document ──< DocumentChunk
                                                         │
                                                    DocumentIndex
                                                         │
                             ChatSession ──< ChatHistory ──< ChatCitation
                             
Experiment ──< BenchmarkResult
     │
  TestSet
  AiModel
  EmbeddingModel
  ChunkingStrategy
```

---



