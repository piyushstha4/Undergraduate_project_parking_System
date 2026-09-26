using System;
using System.Net.Mail;
using BCrypt.Net;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;
using SmartParking.Api.Security;

namespace SmartParking.Api.Services
{
    public sealed class AuthService
    {
        private readonly ParkingDb _db;

        public AuthService(ParkingDb db)
        {
            _db = db;
        }

        public AuthResponse Register(RegisterRequest request)
        {
            if (request == null) throw ApiException.BadRequest("Name is required.");

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) throw ApiException.BadRequest("Name is required.");

            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (!IsEmail(email)) throw ApiException.BadRequest("A valid email is required.");

            if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 6)
            {
                throw ApiException.BadRequest("Password must be at least 6 characters.");
            }

            var role = request.Role == "parking_owner" ? "parking_owner" : "user";
            var existing = _db.Scalar("SELECT id FROM users WHERE email = @p0", email);
            if (existing != null && existing != DBNull.Value)
            {
                throw ApiException.Conflict("An account with this email already exists.");
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10);
            var user = _db.Write((connection, transaction) =>
            {
                ParkingDb.Exec(
                    connection,
                    transaction,
                    "INSERT INTO users (name, email, phone, password_hash, role) VALUES (@p0, @p1, @p2, @p3, @p4)",
                    name,
                    email,
                    string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                    hash,
                    role);
                var id = ParkingDb.LastInsertId(connection, transaction);
                return ParkingDb.QueryOne(connection, transaction, "SELECT * FROM users WHERE id = @p0", Maps.User, id);
            });

            return new AuthResponse { Token = JwtTokenService.Create(user), User = user };
        }

        public AuthResponse Login(LoginRequest request)
        {
            var email = request?.Email?.Trim().ToLowerInvariant();
            if (!IsEmail(email) || string.IsNullOrEmpty(request.Password))
            {
                throw ApiException.BadRequest("Email and password are required.");
            }

            var row = _db.QuerySingle("SELECT * FROM users WHERE email = @p0", r => new UserRow
            {
                User = Maps.User(r),
                Hash = Reader.String(r, "password_hash")
            }, email);

            if (row == null || !BCrypt.Net.BCrypt.Verify(request.Password, row.Hash))
            {
                throw ApiException.Unauthorized("Invalid email or password.");
            }

            if (row.User.Status == "suspended")
            {
                throw ApiException.Forbidden("This account has been suspended. Contact an administrator.");
            }

            return new AuthResponse { Token = JwtTokenService.Create(row.User), User = row.User };
        }

        public UserDto Me(int userId)
        {
            var user = _db.QuerySingle("SELECT * FROM users WHERE id = @p0", Maps.User, userId);
            if (user == null) throw ApiException.NotFound("User not found.");
            return user;
        }

        private static bool IsEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var parsed = new MailAddress(email);
                return !string.IsNullOrEmpty(parsed.Address);
            }
            catch
            {
                return false;
            }
        }

        private sealed class UserRow
        {
            public UserDto User { get; set; }
            public string Hash { get; set; }
        }
    }
}
