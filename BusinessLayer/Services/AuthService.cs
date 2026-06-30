using BusinessLayer.DTOs;
using BusinessLayer.Helpers;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services;

public interface IAuthService
{
    Task<UserDto?> LoginAsync(LoginDto dto);
    Task<UserDto> RegisterAsync(CreateUserDto dto);
    Task<UserDto?> GetByIdAsync(int userId);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<bool> ToggleActiveAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<(int successCount, int skipCount)> ImportUsersFromCsvAsync(Stream csvStream);
    Task<bool> UpdateUserAsync(int userId, UpdateUserDto dto);
    Task<bool> DeleteUserAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AuthService> _logger;
    private readonly IFakeEmailService _emailService;

    public AuthService(IUnitOfWork uow, ILogger<AuthService> logger, IFakeEmailService emailService)
    {
        _uow = uow;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<UserDto?> LoginAsync(LoginDto dto)
    {
        var user = await _uow.Users
            .FirstOrDefaultAsync(u => u.Username == dto.Username && u.IsActive);

        if (user is null) return null;
        if (!SecurityHelper.VerifyPassword(dto.Password, user.PasswordHash)) return null;

        return MapToDto(user);
    }

    public async Task<UserDto> RegisterAsync(CreateUserDto dto)
    {
        // Check uniqueness
        if (await _uow.Users.AnyAsync(u => u.Username == dto.Username))
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");
        if (await _uow.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException($"Email '{dto.Email}' is already registered.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = SecurityHelper.HashPassword(dto.Password),
            Role = dto.Role,
            FullName = dto.FullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("User '{Username}' registered with role '{Role}'", user.Username, user.Role);
        return MapToDto(user);
    }

    public async Task<UserDto?> GetByIdAsync(int userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        return user is null ? null : MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _uow.Users.GetAllAsync();
        return users.Select(MapToDto);
    }

    public async Task<bool> ToggleActiveAsync(int userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user is null) return false;

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user is null) return false;
        if (!SecurityHelper.VerifyPassword(currentPassword, user.PasswordHash)) return false;

        user.PasswordHash = SecurityHelper.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<(int successCount, int skipCount)> ImportUsersFromCsvAsync(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
        
        // Define expected CSV record structure
        var records = csv.GetRecordsAsync<CsvUserRecord>();
        int successCount = 0;
        int skipCount = 0;

        await foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Username) || string.IsNullOrWhiteSpace(record.Email))
            {
                skipCount++;
                continue;
            }

            // Check if user exists
            if (await _uow.Users.AnyAsync(u => u.Username == record.Username || u.Email == record.Email))
            {
                skipCount++;
                continue;
            }

            // Generate a random password if not provided
            var rawPassword = string.IsNullOrWhiteSpace(record.Password) 
                ? Guid.NewGuid().ToString("N")[..8] 
                : record.Password;

            var user = new User
            {
                Username = record.Username,
                Email = record.Email,
                FullName = record.FullName,
                Role = string.IsNullOrWhiteSpace(record.Role) ? "Student" : record.Role,
                PasswordHash = SecurityHelper.HashPassword(rawPassword),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Users.AddAsync(user);
            successCount++;

            // Send notification email
            var subject = "Tài khoản RAG LMS của bạn đã được tạo";
            var body = $@"Xin chào {user.FullName},
Tài khoản đăng nhập hệ thống RAG LMS của bạn đã được khởi tạo.
- Tên đăng nhập: {user.Username}
- Mật khẩu: {rawPassword}
Vui lòng đăng nhập và đổi mật khẩu sớm nhất có thể.
";
            await _emailService.SendEmailAsync(user.Email, subject, body);
        }

        if (successCount > 0)
        {
            await _uow.SaveChangesAsync();
        }

        return (successCount, skipCount);
    }

    private class CsvUserRecord
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    private static UserDto MapToDto(User user) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role,
        FullName = user.FullName,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };

    public async Task<bool> UpdateUserAsync(int userId, UpdateUserDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user is null) return false;

        user.FullName = dto.FullName;
        user.Role = string.IsNullOrWhiteSpace(dto.Role) ? "Student" : dto.Role;
        user.UpdatedAt = DateTime.UtcNow;
        
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user is null) return false;

        _uow.Users.Remove(user);
        await _uow.SaveChangesAsync();
        return true;
    }
}
