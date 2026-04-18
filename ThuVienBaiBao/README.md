# ThuVienBaiBao

Project ASP.NET Core 8 API theo mô hình nhiều tầng, dùng Clean Architecture, CQRS với MediatR, EF Core SQL Server, RabbitMQ, gRPC và MongoDB.

## Cấu trúc

- `ThuVienBaiBao.Api`: Startup project, Swagger, migrate database và seed dữ liệu mẫu.
- `ThuVienBaiBao.Application`: CQRS commands/queries và business logic.
- `ThuVienBaiBao.Contracts`: DTOs và request/response models.
- `ThuVienBaiBao.Domain`: Entity model.
- `ThuVienBaiBao.Persistence`: DbContext, EF mapping, migration, seed data.
- `ThuVienBaiBao.Presentation`: Controllers cho API.
- `Query side read model`: RabbitMQ consumer cập nhật MongoDB, API đọc dữ liệu từ MongoDB.
- `gRPC`: Query API cung cấp read service, Command API có endpoint kiểm tra read model qua gRPC.

## Chức năng

- CRUD `Menu`
- CRUD `News`
- Quan hệ nhiều-nhiều giữa `Menu` và `News`
- Swagger UI để test API
- Dữ liệu mẫu tự động seed khi chạy lần đầu
- Đồng bộ read model sang MongoDB qua RabbitMQ
- Kiểm tra read model qua gRPC

## Cách chạy

1. Khởi động RabbitMQ và MongoDB:

```bash
cd d:\ProjectTest\ThuVienBaiBao
docker compose up -d
```

2. Cấu hình SQL Server trong `ThuVienBaiBao.Api/appsettings.json` nếu máy bạn chưa dùng đúng connection string mặc định.
3. Chạy Query API trước để tạo và nạp read model:

```bash
dotnet run --project .\ThuVienBaiBao.Query.Api\ThuVienBaiBao.Query.Api.csproj --launch-profile https
```

4. Chạy Command API:

```bash
dotnet run --project .\ThuVienBaiBao.Command.Api\ThuVienBaiBao.Command.Api.csproj --launch-profile https
```

5. Mở Swagger theo URL hiển thị trong console.

6. Sau khi tạo hoặc sửa `Menu`/`News` ở Command API, Query API sẽ nhận event qua RabbitMQ và cập nhật MongoDB.

## Database

Project đã có migration đầu tiên ở `ThuVienBaiBao.Persistence/Migrations`.
Nếu muốn update database thủ công, chạy:

```bash
dotnet ef database update --project .\ThuVienBaiBao.Persistence\ThuVienBaiBao.Persistence.csproj --startup-project .\ThuVienBaiBao.Api\ThuVienBaiBao.Api.csproj
```

## API chính

- `GET /api/menus`
- `GET /api/menus/{id}`
- `POST /api/menus`
- `PUT /api/menus/{id}`
- `DELETE /api/menus/{id}`
- `GET /api/news`
- `GET /api/news/{id}`
- `POST /api/news`
- `PUT /api/news/{id}`
- `DELETE /api/news/{id}`

## Tích hợp RabbitMQ, MongoDB, gRPC

### RabbitMQ

Command API phát các event `menu.upserted`, `menu.deleted`, `news.upserted`, `news.deleted` lên exchange `thuvienbaibao.integration`.
Query API chạy consumer nền và cập nhật MongoDB read model.

### MongoDB

Query API đọc dữ liệu từ MongoDB thay vì EF query trực tiếp.
Mongo sẽ được nạp lại từ SQL Server khi Query API khởi động.

### gRPC

Query API expose service `ReadModel` tại HTTPS.
Command API có endpoint kiểm tra:

- `GET /api/integration/menus/{id}`
- `GET /api/integration/news/{id}`

Bạn có thể dùng các endpoint này để xác nhận dữ liệu đã đi từ SQL -> RabbitMQ -> Mongo -> gRPC.

## Cách kiểm tra nhanh

1. Mở RabbitMQ UI: `http://localhost:15672`.
2. Mở MongoDB bằng Compass hoặc shell và xem database `ThuVienBaiBaoReadModel`.
3. Gửi `POST /api/menus` hoặc `POST /api/news` ở Command API.
4. Chờ 1-2 giây rồi gọi lại `GET /api/integration/menus/{id}` hoặc `GET /api/integration/news/{id}` ở Command API.
5. Gọi `GET /api/menus` và `GET /api/news` ở Query API để xem read model đã cập nhật.

## Seed dữ liệu mẫu

Khi app chạy lần đầu và database còn trống, project sẽ tạo sẵn vài menu và bài viết mẫu để demo quan hệ n-n.
