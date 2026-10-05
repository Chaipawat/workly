# Workly Auth API

## ลองด้วย Swagger

รัน PostgreSQL และ API จาก root ของ repository:

```powershell
docker compose up -d postgres
dotnet run --project apps/api/src/Workly.Api --launch-profile http
```

เปิด `http://localhost:5237/swagger/index.html` แล้วทดลองตามลำดับ:

1. `POST /api/auth/register` กด Try it out แล้วส่งข้อมูลตัวอย่าง:

   ```json
   {
     "email": "student@example.com",
     "password": "My-learning-password-42",
     "displayName": "Student"
   }
   ```

2. เก็บ `accessToken` และ `refreshToken` จาก response สำหรับทดลอง
3. กด Authorize และวางเฉพาะ access token ไม่ต้องเติมคำว่า Bearer
4. เรียก `GET /api/auth/me` เพื่อดูบัญชีที่ login อยู่
5. เรียก `POST /api/auth/refresh` โดยส่ง `{ "refreshToken": "..." }`
6. ใช้ refresh token ใหม่จาก response ครั้งถัดไปเสมอ
7. เรียก `POST /api/auth/logout` โดยส่ง refresh token ของ session ที่ต้องการออก

Login ใช้ `POST /api/auth/login` ด้วย email และ password ของบัญชีที่สมัครไว้
Register ยังไม่สร้าง Organization หรือ role ให้ผู้ใช้

## ทำไมมีแต่ละชั้น

- `Application/Auth/AuthContracts.cs`: DTOs สำหรับ request/response และ `IAuthService`.
  Request DTO รับ password แต่ response DTO ไม่ส่ง password hash กลับไป.
- `Api/Controllers/AuthController.cs`: รับ HTTP, ใช้ model validation และเรียก service.
- `Infrastructure/Auth/AuthService.cs`: ทำ use cases โดยใช้ DbContext, password hasher
  และ token cryptography. อยู่ใน Infrastructure เพราะใช้ EF Core และ technical
  dependencies โดยตรง; Application ประกาศ contract ให้ API เรียกผ่าน DI.
- `Infrastructure/Auth/JwtSettings.cs`: ค่าที่ใช้ร่วมกันระหว่างการออกและตรวจ JWT.
- `Api/AuthExceptionHandler.cs`: เปลี่ยน expected auth failures เป็น ProblemDetails.
- `Program.cs`: ลงทะเบียน DI, authentication, authorization, rate limiting และ Swagger.

Flow จริง: Controller → IAuthService/AuthService → WorklyDbContext → PostgreSQL.
ไม่มี Repository pattern ซ้อน EF Core และไม่ใช้ MediatR.

## Password และ tokens

Password ใช้ ASP.NET Core `PasswordHasher<User>` ซึ่งมี salt และ password hashing.
Login ตรวจ hash และอัปเกรด hash หาก library แจ้งว่า rehash จำเป็น.

Access token เป็น JWT ที่ลงลายเซ็น HS256 อายุ 15 นาที. API ตรวจ signature,
issuer, audience และ expiry โดยมี clock skew 30 วินาที. JWT ไม่ใช่ข้อมูลเข้ารหัส;
อย่าใส่ password หรือข้อมูลลับใน claims. Token มี user ID และ token ID, ไม่มี org role.

Refresh token เป็น random 256-bit value อายุ session 7 วัน. Database เก็บ SHA-256
hash เท่านั้น. SHA-256 ใช้กับ random token ไม่ใช่กับ password.

Refresh ใช้ transaction และล็อกแถว user ก่อนตรวจ token อีกครั้ง. Token เดิมถูก revoke
และเชื่อมไป token ใหม่ผ่าน `ReplacedById`. Rotation ไม่ขยายวันหมดอายุของ session.
การใช้ rotated token เก่าซ้ำจะ revoke refresh tokens ของบัญชีทั้งหมด รวม session อื่น.
Client ต้องจัด refresh request ให้เกิดครั้งเดียวในเวลาเดียวกัน (single flight).

Logout revoke session และ replacements ของ session นั้น. ไม่ได้ revoke JWT ที่ออกไปแล้ว;
JWT ยังใช้ได้จนหมดอายุ. Logout endpoint คืน 204 ซ้ำได้แม้ token ถูก revoke แล้ว.

Auth endpoints จำกัด 30 requests ต่อนาทีต่อ remote IP ในแต่ละ API process.
เมื่อ deploy หลัง proxy ต้องตั้ง trusted forwarded headers และพิจารณา distributed limits.

## Signing key

Development ที่ไม่กำหนด key จะสร้าง random key ใน memory ทุกครั้งที่ API เริ่ม.
JWT เก่าจึงใช้ไม่ได้หลัง restart; refresh token ยังใช้ขอ JWT ใหม่ได้.
ต้องการ key คงที่ให้ตั้ง `Jwt__SigningKey` ผ่าน environment variable หรือ user secrets.
Production ต้องกำหนด random secret อย่างน้อย 32 bytes; API จะไม่เริ่มถ้าไม่มี key.
อย่า commit key จริงลง Git และใช้ HTTPS เมื่อส่ง credentials/tokens นอก local development.

## Tests

`apps/api/tests/auth-smoke.ps1` ตรวจ HTTP flow และ secret hashes บน PostgreSQL จริง.
ต้องใช้ API ที่ต่อ database ชื่อ `workly_auth_test_<hex>` แยกจากฐานข้อมูล development.
ตัวอย่างคำสั่งเมื่อเตรียม API ทดสอบที่ port 5248 แล้ว:

```powershell
./apps/api/tests/auth-smoke.ps1 -DatabaseName workly_auth_test_abcd
```

ตรวจ Register/Login, validation, duplicate email, tampered JWT, refresh rotation/replay,
concurrent refresh, Logout, refresh expiry, stored hashes และ rate limiting.
Script ใช้บัญชีทดสอบและปรับ expiry ใน database ทดสอบเท่านั้น.

ยังไม่มีหน้า Login/Register ใน Nuxt, organization membership authorization,
email verification หรือ password reset. Frontend ยังไม่เก็บ token; ตอนเชื่อม browser
จะออกแบบ refresh cookie/CSRF หรือ Nuxt server session ก่อน ไม่เก็บ refresh token
ใน localStorage โดยอัตโนมัติ.

References:
- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0
- https://www.rfc-editor.org/rfc/rfc9700.html
