using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using wspolpracujmy.Data;
using wspolpracujmy.Services;
using Microsoft.AspNetCore.Authorization;
using wspolpracujmy.Services.Authorization;

var builder = WebApplication.CreateBuilder(args);


const string AllowFrontend = "AllowFrontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(AllowFrontend, policy =>
    {
        policy.WithOrigins(
                "http://localhost:5500",
                "http://127.0.0.1:5500"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Wpisz token JWT w formacie: Bearer {token}"
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application services
builder.Services.AddScoped<wspolpracujmy.Services.ProjectService>();
builder.Services.AddScoped<wspolpracujmy.Services.ProjectCommentService>();
builder.Services.AddScoped<wspolpracujmy.Services.NotificationService>();
builder.Services.AddScoped<wspolpracujmy.Services.GroupRequestService>();

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<GroupAuthorizationService>();
builder.Services.AddScoped<IAuthorizationHandler, GroupOwnerHandler>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new ArgumentNullException("SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "wspolpracujmy",
        ValidAudience = jwtSettings["Audience"] ?? "wspolpracujmy",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CompanyOnly", policy => policy.RequireRole("Company", "Admin"));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
    options.AddPolicy("GroupOwner", policy => policy.Requirements.Add(new GroupOwnerRequirement()));
});

var app = builder.Build();

const string migrationProductVersion = "10.0.5";
var baselineMigrationIds = new[]
{
    "20260425133944_FixNotificationIdentity",
    "20260505222156_InitialCreate",
    "20260505232644_MakeStudentGroupIdNullable",
    "20260506153000_AddGroupRequestRelationsAndOptionalGroupProject",
    "20260506154500_MakeGroupIsAcceptedNullable",
    "20260506161000_MakeStudentGroupOptional",
    "20260506210803_SyncPendingModelChanges",
    "20260506223004_MakeGroupRequestStudentIdNullable",
    "20260507002252_AddGroupRequestUniqueIndexes",
    "20260507120000_AddMissingGroupRequestFks",
    "20260507210134_AddGroupMaxMembers",
    "20260508172844_AutoMigrationForModels",
    "20260508215419_RefactorGroupRequestNotifications"
};

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
            ""MigrationId"" character varying(150) NOT NULL,
            ""ProductVersion"" character varying(32) NOT NULL,
            CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
        );");

    foreach (var migrationId in baselineMigrationIds)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            VALUES ({migrationId}, {migrationProductVersion})
            ON CONFLICT (""MigrationId"") DO NOTHING;");
    }

    await db.Database.MigrateAsync();
}

// (dev-only truncation was run temporarily and removed)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Seed development data (runs only in Development)
    // First run the dedicated TagsSeeder so tag names are controlled separately.
    await TagsSeeder.SeedAsync(app);
    await TestDataSeeder.SeedAsync(app);
}
app.UseCors(AllowFrontend);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();