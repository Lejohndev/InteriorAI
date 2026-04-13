InteriorAI.API/             
    ├── Controllers/              # Endpoints: AuthController, DesignController, WebhookController
    ├── Models/                   # Gộp chung DB Entities và DTOs
    │   ├── Entities/             # Cấu trúc bảng DB: User, Project, DesignResult
    │   └── DTOs/                 # Data transfer: DesignRequest, DesignResponse
    ├── Services/                 # Chứa logic nghiệp vụ và gọi API ngoài
    │   ├── AuthManager.cs        # Xử lý đăng nhập, cấp JWT
    │   ├── DesignManager.cs      # Logic lưu ảnh, gọi AI, cập nhật trạng thái
    │   └── ReplicateClient.cs    # Triển khai gọi API Replicate/Leonardo
    ├── Data/                     # AppDbContext và Migrations
    ├── Extensions/               # ServiceCollectionExtensions (setup JWT, CORS)
    ├── appsettings.json
    └── Program.cs