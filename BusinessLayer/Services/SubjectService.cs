using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services;

public interface ISubjectService
{
    Task<IEnumerable<SubjectDto>> GetAllAsync();
    Task<SubjectDto?> GetByIdAsync(int subjectId);
    Task<SubjectDto> CreateAsync(CreateSubjectDto dto);
    Task<bool> UpdateAsync(int subjectId, CreateSubjectDto dto);
    Task<bool> DeleteAsync(int subjectId);
    Task<IEnumerable<ChapterDto>> GetChaptersAsync(int subjectId);
    Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto);
    Task<bool> UpdateChapterAsync(int chapterId, CreateChapterDto dto);
    Task<bool> DeleteChapterAsync(int chapterId);
    Task<IEnumerable<EmbeddingModelDto>> GetEmbeddingModelsAsync();
    Task<IEnumerable<ChunkingStrategyDto>> GetChunkingStrategiesAsync();
    Task<IEnumerable<UserDto>> GetTeachersAsync();
    Task<IEnumerable<int>> GetAssignedTeacherIdsAsync(int subjectId);
    Task<IEnumerable<int>> GetHeadTeacherIdsAsync(int subjectId);
    Task<bool> IsSubjectHeadAsync(int teacherId, int subjectId);
    Task<bool> AssignTeachersAsync(int subjectId, List<int> teacherIds, List<int> headTeacherIds);
    Task<IEnumerable<SubjectDto>> GetTeacherSubjectsAsync(int teacherId);
}

public class SubjectService : ISubjectService
{
    private readonly IUnitOfWork _uow;

    public SubjectService(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<SubjectDto>> GetAllAsync()
    {
        return await _uow.Subjects.Query()
            .Where(s => s.IsActive)
            .Select(s => new SubjectDto
            {
                SubjectId = s.SubjectId,
                SubjectCode = s.SubjectCode,
                SubjectName = s.SubjectName,
                Description = s.Description,
                IsActive = s.IsActive,
                ChapterCount = s.Chapters.Count,
                DocumentCount = s.Chapters.SelectMany(c => c.Documents).Count()
            })
            .ToListAsync();
    }

    public async Task<SubjectDto?> GetByIdAsync(int subjectId)
    {
        return await _uow.Subjects.Query()
            .Where(s => s.SubjectId == subjectId)
            .Select(s => new SubjectDto
            {
                SubjectId = s.SubjectId,
                SubjectCode = s.SubjectCode,
                SubjectName = s.SubjectName,
                Description = s.Description,
                IsActive = s.IsActive,
                ChapterCount = s.Chapters.Count,
                DocumentCount = s.Chapters.SelectMany(c => c.Documents).Count()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<SubjectDto> CreateAsync(CreateSubjectDto dto)
    {
        var subject = new Subject
        {
            SubjectCode = dto.SubjectCode,
            SubjectName = dto.SubjectName,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Subjects.AddAsync(subject);
        await _uow.SaveChangesAsync();
        return await GetByIdAsync(subject.SubjectId) ?? throw new Exception("Subject not found after creation");
    }

    public async Task<bool> UpdateAsync(int subjectId, CreateSubjectDto dto)
    {
        var subject = await _uow.Subjects.GetByIdAsync(subjectId);
        if (subject is null) return false;

        subject.SubjectCode = dto.SubjectCode;
        subject.SubjectName = dto.SubjectName;
        subject.Description = dto.Description;
        _uow.Subjects.Update(subject);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int subjectId)
    {
        var subject = await _uow.Subjects.GetByIdAsync(subjectId);
        if (subject is null) return false;

        subject.IsActive = false;
        _uow.Subjects.Update(subject);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ChapterDto>> GetChaptersAsync(int subjectId)
    {
        return await _uow.Chapters.Query()
            .Where(c => c.SubjectId == subjectId)
            .OrderBy(c => c.OrderIndex)
            .Select(c => new ChapterDto
            {
                ChapterId = c.ChapterId,
                SubjectId = c.SubjectId,
                SubjectName = c.Subject.SubjectName,
                ChapterName = c.ChapterName,
                OrderIndex = c.OrderIndex,
                Description = c.Description,
                DocumentCount = c.Documents.Count,
                Documents = c.Documents.Select(d => new DocumentDto
                {
                    DocumentId = d.DocumentId,
                    ChapterId = d.ChapterId,
                    ChapterName = c.ChapterName,
                    SubjectName = c.Subject.SubjectName,
                    FileName = d.FileName,
                    OriginalFileName = d.OriginalFileName,
                    FileType = d.FileType,
                    FileSizeBytes = d.FileSizeBytes,
                    Status = d.Status,
                    ErrorMessage = d.ErrorMessage,
                    TotalChunks = d.TotalChunks,
                    UploadedAt = d.UploadedAt,
                    IndexedAt = d.IndexedAt,
                    UploadedByFullName = d.UploadedBy.FullName
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto)
    {
        var chapter = new Chapter
        {
            SubjectId = dto.SubjectId,
            ChapterName = dto.ChapterName,
            OrderIndex = dto.OrderIndex,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Chapters.AddAsync(chapter);
        await _uow.SaveChangesAsync();

        return await _uow.Chapters.Query()
            .Where(c => c.ChapterId == chapter.ChapterId)
            .Select(c => new ChapterDto
            {
                ChapterId = c.ChapterId,
                SubjectId = c.SubjectId,
                SubjectName = c.Subject.SubjectName,
                ChapterName = c.ChapterName,
                OrderIndex = c.OrderIndex,
                Description = c.Description,
                DocumentCount = 0,
                Documents = new List<DocumentDto>()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateChapterAsync(int chapterId, CreateChapterDto dto)
    {
        var chapter = await _uow.Chapters.GetByIdAsync(chapterId);
        if (chapter is null) return false;

        chapter.ChapterName = dto.ChapterName;
        chapter.OrderIndex = dto.OrderIndex;
        chapter.Description = dto.Description;
        _uow.Chapters.Update(chapter);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteChapterAsync(int chapterId)
    {
        var chapter = await _uow.Chapters.GetByIdAsync(chapterId);
        if (chapter is null) return false;

        _uow.Chapters.Remove(chapter);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<EmbeddingModelDto>> GetEmbeddingModelsAsync()
    {
        return await _uow.EmbeddingModels.Query()
            .Where(m => m.IsActive)
            .Select(m => new EmbeddingModelDto
            {
                EmbeddingModelId = m.EmbeddingModelId,
                ModelName = m.ModelName,
                Provider = m.Provider,
                VectorDimension = m.VectorDimension,
                IsDefault = m.IsDefault,
                IsActive = m.IsActive
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ChunkingStrategyDto>> GetChunkingStrategiesAsync()
    {
        return await _uow.ChunkingStrategies.Query()
            .Where(s => s.IsActive)
            .Select(s => new ChunkingStrategyDto
            {
                ChunkingStrategyId = s.ChunkingStrategyId,
                StrategyName = s.StrategyName,
                StrategyType = s.StrategyType,
                ChunkSize = s.ChunkSize,
                ChunkOverlap = s.ChunkOverlap,
                IsDefault = s.IsDefault
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<UserDto>> GetTeachersAsync()
    {
        return await _uow.Users.Query()
            .Where(u => u.Role == "Teacher" && u.IsActive)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<int>> GetAssignedTeacherIdsAsync(int subjectId)
    {
        return await _uow.SubjectTeachers.Query()
            .Where(st => st.SubjectId == subjectId)
            .Select(st => st.UserId)
            .ToListAsync();
    }

    public async Task<IEnumerable<int>> GetHeadTeacherIdsAsync(int subjectId)
    {
        return await _uow.SubjectTeachers.Query()
            .Where(st => st.SubjectId == subjectId && st.IsSubjectHead)
            .Select(st => st.UserId)
            .ToListAsync();
    }

    public async Task<bool> IsSubjectHeadAsync(int teacherId, int subjectId)
    {
        return await _uow.SubjectTeachers.Query()
            .AnyAsync(st => st.UserId == teacherId && st.SubjectId == subjectId && st.IsSubjectHead);
    }

    public async Task<bool> AssignTeachersAsync(int subjectId, List<int> teacherIds, List<int> headTeacherIds)
    {
        // Get current assignments
        var currentAssignments = await _uow.SubjectTeachers.Query()
            .Where(st => st.SubjectId == subjectId)
            .ToListAsync();

        // Remove unselected
        var toRemove = currentAssignments.Where(st => !teacherIds.Contains(st.UserId)).ToList();
        foreach (var r in toRemove)
        {
            _uow.SubjectTeachers.Remove(r);
        }

        // Update existing & Add new
        var currentTeacherIds = currentAssignments.Select(st => st.UserId).ToList();
        
        foreach (var id in teacherIds)
        {
            var existing = currentAssignments.FirstOrDefault(st => st.UserId == id);
            bool isHead = headTeacherIds.Contains(id);
            
            if (existing != null)
            {
                existing.IsSubjectHead = isHead;
                _uow.SubjectTeachers.Update(existing);
            }
            else
            {
                await _uow.SubjectTeachers.AddAsync(new SubjectTeacher
                {
                    SubjectId = subjectId,
                    UserId = id,
                    IsSubjectHead = isHead
                });
            }
        }

        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<SubjectDto>> GetTeacherSubjectsAsync(int teacherId)
    {
        return await _uow.SubjectTeachers.Query()
            .Where(st => st.UserId == teacherId && st.Subject.IsActive)
            .Select(st => new SubjectDto
            {
                SubjectId = st.Subject.SubjectId,
                SubjectCode = st.Subject.SubjectCode,
                SubjectName = st.Subject.SubjectName,
                Description = st.Subject.Description,
                IsActive = st.Subject.IsActive,
                ChapterCount = st.Subject.Chapters.Count,
                DocumentCount = st.Subject.Chapters.SelectMany(c => c.Documents).Count()
            })
            .ToListAsync();
    }
}
