# Remaining Fees Module - Best Practices & Optimization

## Database Optimization

### 1. Essential Indexes

Create these indexes for optimal query performance:

```sql
-- Student-related indexes
CREATE INDEX idx_student_classid ON "Students"("ClassId");
CREATE INDEX idx_student_divisionid ON "Students"("DivisionId");
CREATE INDEX idx_student_branchid ON "Students"("BranchId");
CREATE INDEX idx_student_isactive ON "Students"("IsActive");

-- Payment-related indexes
CREATE INDEX idx_payment_studentid ON "Payments"("StudentId");
CREATE INDEX idx_payment_branchid ON "Payments"("BranchId");
CREATE INDEX idx_payment_date ON "Payments"("PaymentDate");

-- ClassFeeMap indexes
CREATE INDEX idx_classfee_classid ON "ClassFeeMap"("ClassId");
CREATE INDEX idx_classfee_academicyearid ON "ClassFeeMap"("AcademicYearId");
CREATE INDEX idx_classfee_classid_academicyearid ON "ClassFeeMap"("ClassId", "AcademicYearId");

-- PaymentFeeLine indexes
CREATE INDEX idx_paymentline_paymentid ON "PaymentFeeLine"("PaymentId");
CREATE INDEX idx_paymentline_feeheadid ON "PaymentFeeLine"("FeeHeadId");

-- Class and Division indexes
CREATE INDEX idx_class_branchid ON "Class"("BranchId");
CREATE INDEX idx_division_classid ON "Division"("ClassId");
CREATE INDEX idx_division_branchid ON "Division"("BranchId");

-- Composite indexes for common queries
CREATE INDEX idx_student_class_academicyear ON "Students"("ClassId", "AcademicYearId");
CREATE INDEX idx_payment_studentid_date ON "Payments"("StudentId", "PaymentDate");
```

### 2. Query Optimization

For better performance with large datasets:

```csharp
// Instead of loading all students and iterating:
// ❌ BAD: var students = await _context.Students.ToListAsync();
// foreach (var student in students) { ... }

// ✅ GOOD: Use projection and batch processing
var studentsWithFees = await _context.Students
    .AsNoTracking()
    .Where(s => s.IsActive == true)
    .Select(s => new StudentFeeDto
    {
        Id = s.Id,
        Name = s.FirstName + " " + s.LastName,
        ClassId = s.ClassId,
        DivisionId = s.DivisionId
    })
    .ToListAsync();
```

### 3. Caching Strategy

Implement caching for frequently accessed data:

```csharp
public class RemainingFeesService : IRemainingFeesService
{
    private readonly IMemoryCache _cache;
    private const string CACHE_KEY_PREFIX = "fees_";
    private const int CACHE_DURATION_MINUTES = 30;

    public async Task<StudentRemainingFeesDto> GetStudentRemainingFeesAsync(Guid studentId)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}student_{studentId}";
        
        if (_cache.TryGetValue(cacheKey, out StudentRemainingFeesDto cachedResult))
        {
            return cachedResult;
        }

        // ... existing code ...

        // Cache the result
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
        return result;
    }

    // Clear cache when payment is recorded
    public void InvalidateStudentCache(Guid studentId)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}student_{studentId}";
        _cache.Remove(cacheKey);
    }
}
```

### 4. Materialized Views for Analytics

For frequently queried analytics data:

```sql
CREATE MATERIALIZED VIEW v_student_remaining_fees AS
SELECT 
    s."Id" as StudentId,
    s."FirstName" || ' ' || COALESCE(s."LastName", '') as StudentName,
    s."ClassId",
    s."DivisionId",
    s."BranchId",
    COALESCE(SUM(cfm."Amount"), 0) as TotalDueFees,
    COALESCE(SUM(p."Amount"), 0) as PaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(p."Amount"), 0) as RemainingFees
FROM 
    public."Students" s
LEFT JOIN 
    public."ClassFeeMap" cfm ON s."ClassId" = cfm."ClassId"
LEFT JOIN 
    public."Payments" p ON s."Id" = p."StudentId"
WHERE 
    s."IsActive" = true
GROUP BY 
    s."Id", s."FirstName", s."LastName", s."ClassId", s."DivisionId", s."BranchId";

CREATE INDEX idx_v_student_fees_remaining ON v_student_remaining_fees("RemainingFees" DESC);

-- Refresh the materialized view (schedule this periodically)
REFRESH MATERIALIZED VIEW CONCURRENTLY v_student_remaining_fees;
```

---

## Performance Optimization Techniques

### 1. Pagination for Large Datasets

```csharp
public async Task<PagedResponse<ClassRemainingFeesDto>> GetAllClassesRemainingFeesAsync(
    int pageNumber = 1, 
    int pageSize = 10)
{
    var totalCount = await _context.Classes.CountAsync();
    
    var classes = await _context.Classes
        .AsNoTracking()
        .OrderBy(c => c.Name)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var result = new List<ClassRemainingFeesDto>();
    foreach (var classEntity in classes)
    {
        result.Add(await GetClassRemainingFeesAsync(classEntity.Id));
    }

    return new PagedResponse<ClassRemainingFeesDto>
    {
        Data = result,
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
    };
}
```

### 2. Asynchronous Batch Processing

```csharp
public async Task<IEnumerable<StudentRemainingFeesDto>> GetAllStudentsRemainingFeesAsync(
    int batchSize = 100)
{
    var totalStudents = await _context.Students.CountAsync();
    var result = new List<StudentRemainingFeesDto>();

    for (int i = 0; i < totalStudents; i += batchSize)
    {
        var studentBatch = await _context.Students
            .AsNoTracking()
            .Skip(i)
            .Take(batchSize)
            .Select(s => s.Id)
            .ToListAsync();

        var batchFees = await Task.WhenAll(
            studentBatch.Select(id => GetStudentRemainingFeesAsync(id))
        );

        result.AddRange(batchFees);
    }

    return result;
}
```

### 3. Raw SQL Queries for Performance-Critical Operations

```csharp
public async Task<List<StudentRemainingFeesDto>> GetStudentRemainingFeesFastAsync(Guid studentId)
{
    var sql = @"
        SELECT 
            s.""Id"" as StudentId,
            s.""FirstName"" || ' ' || COALESCE(s.""LastName"", '') as StudentName,
            s.""AdmissionNo"",
            s.""ClassId"",
            s.""DivisionId"",
            s.""BranchId"",
            COALESCE(SUM(cfm.""Amount""), 0) as TotalDueFees,
            COALESCE(SUM(p.""Amount""), 0) as PaidAmount
        FROM 
            public.""Students"" s
        LEFT JOIN 
            public.""ClassFeeMap"" cfm ON s.""ClassId"" = cfm.""ClassId""
        LEFT JOIN 
            public.""Payments"" p ON s.""Id"" = p.""StudentId""
        WHERE 
            s.""Id"" = @studentId
        GROUP BY 
            s.""Id"", s.""FirstName"", s.""LastName"", s.""AdmissionNo"", 
            s.""ClassId"", s.""DivisionId"", s.""BranchId""
    ";

    var result = await _context.StudentRemainingFees
        .FromSqlInterpolated($"{sql}", studentId)
        .AsNoTracking()
        .ToListAsync();

    return result;
}
```

---

## Code Optimization

### 1. Reduce Database Queries

```csharp
// ❌ BAD: N+1 Query Problem
public async Task<IEnumerable<ClassRemainingFeesDto>> GetAllClassesBadAsync()
{
    var classes = await _context.Classes.ToListAsync();
    var result = new List<ClassRemainingFeesDto>();
    
    foreach (var classEntity in classes)
    {
        // This query runs for EACH class
        var students = await _context.Students
            .Where(s => s.ClassId == classEntity.Id)
            .ToListAsync();
        
        var fees = await _context.ClassFeeMaps
            .Where(cfm => cfm.ClassId == classEntity.Id)
            .ToListAsync();
        
        // ... more queries ...
    }
    return result;
}

// ✅ GOOD: Use joins and projections
public async Task<IEnumerable<ClassRemainingFeesDto>> GetAllClassesGoodAsync()
{
    var result = await _context.Classes
        .AsNoTracking()
        .Select(c => new ClassRemainingFeesDto
        {
            ClassId = c.Id,
            ClassName = c.Name,
            BranchId = c.BranchId,
            TotalStudents = _context.Students
                .Where(s => s.ClassId == c.Id).Count(),
            TotalClassDueFees = _context.ClassFeeMaps
                .Where(cfm => cfm.ClassId == c.Id)
                .Sum(cfm => cfm.Amount ?? 0),
            TotalPaidAmount = _context.Payments
                .Where(p => _context.Students
                    .Where(s => s.ClassId == c.Id)
                    .Select(s => s.Id)
                    .Contains(p.StudentId ?? Guid.Empty))
                .Sum(p => p.Amount ?? 0)
        })
        .ToListAsync();

    return result;
}
```

### 2. Use Select and AsNoTracking

```csharp
// For read-only operations, use AsNoTracking for better performance
var students = await _context.Students
    .AsNoTracking()
    .Where(s => s.IsActive == true)
    .Select(s => new { s.Id, s.FirstName, s.LastName })
    .ToListAsync();
```

---

## Monitoring & Logging

### 1. Query Performance Monitoring

```csharp
public class RemainingFeesService : IRemainingFeesService
{
    private readonly ILogger<RemainingFeesService> _logger;

    public async Task<StudentRemainingFeesDto> GetStudentRemainingFeesAsync(Guid studentId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation($"Fetching remaining fees for student: {studentId}");
            var result = await GetStudentRemainingFeesAsyncInternal(studentId);
            
            stopwatch.Stop();
            _logger.LogInformation(
                $"Student fees retrieved in {stopwatch.ElapsedMilliseconds}ms for student: {studentId}"
            );
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                $"Error fetching student fees in {stopwatch.ElapsedMilliseconds}ms: {ex.Message}"
            );
            throw;
        }
    }
}
```

### 2. DbContext Query Logging

```csharp
// In Program.cs
builder.Services.AddDbContext<BonzerSchoolDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection"));
    
    // Enable query logging in development
    if (builder.Environment.IsDevelopment())
    {
        options
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, LogLevel.Information);
    }
});
```

---

## API Best Practices

### 1. Versioning

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class RemainingFeesController : ControllerBase
{
    // Allows for future API version changes without breaking existing clients
}
```

### 2. Rate Limiting

```csharp
// In Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
    });
});
```

### 3. Response Compression

```csharp
// In Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json" });
});

app.UseResponseCompression();
```

---

## Testing Strategies

### 1. Unit Tests

```csharp
[TestFixture]
public class RemainingFeesServiceTests
{
    private RemainingFeesService _service;
    private Mock<BonzerSchoolDbContext> _contextMock;
    private Mock<ILogger<RemainingFeesService>> _loggerMock;

    [SetUp]
    public void Setup()
    {
        _contextMock = new Mock<BonzerSchoolDbContext>();
        _loggerMock = new Mock<ILogger<RemainingFeesService>>();
        _service = new RemainingFeesService(_contextMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task GetStudentRemainingFees_WithValidStudent_ReturnsCorrectFees()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var expectedFees = 20000m;

        // Mock data setup
        _contextMock.Setup(c => c.Students)
            .Returns(new[] { new Student { Id = studentId } }.AsQueryable());

        // Act
        var result = await _service.GetStudentRemainingFeesAsync(studentId);

        // Assert
        Assert.NotNull(result);
        Assert.AreEqual(studentId, result.StudentId);
    }
}
```

### 2. Integration Tests

```csharp
[TestFixture]
public class RemainingFeesIntegrationTests
{
    private BonzerSchoolDbContext _dbContext;
    private RemainingFeesService _service;

    [SetUp]
    public async Task Setup()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<BonzerSchoolDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new BonzerSchoolDbContext(options);
        _service = new RemainingFeesService(_dbContext, LoggerMock.Object);
        
        // Seed test data
        await SeedTestData();
    }

    [Test]
    public async Task GetStudentRemainingFees_ReturnsAccurateCalculations()
    {
        // Test implementation
    }
}
```

---

## Security Considerations

### 1. Input Validation

```csharp
[HttpGet("student/{studentId}")]
public async Task<IActionResult> GetStudentRemainingFees(Guid studentId)
{
    // Validate input
    if (studentId == Guid.Empty)
    {
        return BadRequest("Invalid student ID");
    }

    // Add authorization check
    var currentUser = User.FindFirst(ClaimTypes.NameIdentifier);
    // Verify user has access to this student's data

    var result = await _remainingFeesService.GetStudentRemainingFeesAsync(studentId);
    return Ok(new RemainingFeesResponse { Data = result });
}
```

### 2. Data Sanitization

```csharp
// Use parameterized queries (already done with EF Core)
// Avoid string concatenation in SQL
```

---

## Migration & Deployment

### 1. Database Migration Script

```bash
# Create migration
dotnet ef migrations add AddRemainingFeesIndexes -p Infrastructure

# Apply migration
dotnet ef database update -p Infrastructure
```

### 2. Health Check Configuration

```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BonzerSchoolDbContext>();

// In middleware
app.MapHealthChecks("/health");
```

---

## Monitoring & Alerting

### 1. Application Insights

```csharp
// In Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// In service
_telemetryClient.TrackEvent("StudentFeesRetrieved", 
    properties: new Dictionary<string, string>
    {
        {"StudentId", studentId.ToString()},
        {"RemainingFees", studentFees.RemainingFees.ToString()}
    });
```

### 2. Logging Best Practices

```csharp
_logger.LogInformation(
    "Student {StudentId} has remaining fees of {RemainingFees}",
    studentId,
    studentFees.RemainingFees
);

_logger.LogWarning(
    "High pending fees detected for student {StudentId}: {Amount}",
    studentId,
    pendingAmount
);
```

---

## Conclusion

Following these best practices will ensure:
- ✅ Better performance with large datasets
- ✅ Improved user experience
- ✅ Easier maintenance and debugging
- ✅ Enhanced security
- ✅ Better monitoring and alerting
