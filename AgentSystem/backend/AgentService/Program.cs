using System.Text;
using AgentCore.CodeMap;
using AgentCore.Embedding;
using AgentCore.Services;
using AgentInfrastructure.Clients;
using AgentInfrastructure.Data;
using AgentInfrastructure.Repositories;
using AgentInfrastructure.Tools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load secrets: secrets.json (commit, placeholder) → secrets.local.json (gitignore, key thật) override
builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: false);
builder.Configuration.AddJsonFile("secrets.local.json", optional: true, reloadOnChange: false);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database
builder.Services.AddDbContext<AgentDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// JWT auth
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Infrastructure
builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<IOrchestratorLogRepository, OrchestratorLogRepository>();
builder.Services.AddScoped<ISpecRepository, SpecRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUsageRepository, UsageRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddHttpClient<IClaudeClient, ClaudeClient>(); // real Claude API

// Core services
builder.Services.AddSingleton<IAgentLoader, AgentLoader>();
builder.Services.AddScoped<ISpecService, SpecService>();
builder.Services.AddScoped<IAgentService, AgentCore.Services.AgentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsageService, UsageService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddSingleton<IToolService, ToolService>();
builder.Services.AddHttpClient<IOrchestratorService, OrchestratorService>();

// CodeMap
builder.Services.AddSingleton<RoslynAnalyzer>();
builder.Services.AddSingleton<TypeScriptAnalyzer>();
builder.Services.AddScoped<ICodeMapService, CodeMapService>();

// Embedding (semantic code search — complement to CodeMap)
builder.Services.AddSingleton<CodeChunker>();
builder.Services.AddSingleton<OnnxEmbedder>();
builder.Services.AddScoped<VectorStore>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

// Auto-apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
    try { db.Database.Migrate(); } catch { /* logged below */ }

    // Drop old Specs if it still has the old JsonSpec column, then recreate new schema
    db.Database.ExecuteSqlRaw(@"
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Specs') AND name = 'JsonSpec')
        BEGIN
            IF OBJECT_ID('SpecTaskVersions','U') IS NOT NULL DROP TABLE [SpecTaskVersions];
            IF OBJECT_ID('SpecTasks','U') IS NOT NULL DROP TABLE [SpecTasks];
            DROP TABLE [Specs];
        END

        IF OBJECT_ID('Specs','U') IS NULL
            CREATE TABLE [Specs] (
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [SessionId] uniqueidentifier NOT NULL,
                [Name] nvarchar(500) NOT NULL,
                [Description] nvarchar(1000) NOT NULL DEFAULT '',
                [CreatedAt] datetime2 NOT NULL
            );

        IF OBJECT_ID('SpecTasks','U') IS NULL
            CREATE TABLE [SpecTasks] (
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [SpecId] uniqueidentifier NOT NULL,
                [Name] nvarchar(500) NOT NULL,
                [Description] nvarchar(1000) NOT NULL DEFAULT '',
                [CreatedAt] datetime2 NOT NULL,
                CONSTRAINT [FK_SpecTasks_Specs_SpecId] FOREIGN KEY ([SpecId]) REFERENCES [Specs]([Id]) ON DELETE CASCADE
            );

        IF OBJECT_ID('SpecTaskVersions','U') IS NULL
            CREATE TABLE [SpecTaskVersions] (
                [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                [TaskId] uniqueidentifier NOT NULL,
                [JsonSpec] nvarchar(max) NOT NULL,
                [Version] int NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                CONSTRAINT [FK_SpecTaskVersions_SpecTasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [SpecTasks]([Id]) ON DELETE CASCADE
            );

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SpecTasks_SpecId')
            CREATE INDEX [IX_SpecTasks_SpecId] ON [SpecTasks]([SpecId]);

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SpecTaskVersions_TaskId_Version')
            CREATE UNIQUE INDEX [IX_SpecTaskVersions_TaskId_Version] ON [SpecTaskVersions]([TaskId],[Version]);
    ");
}

// app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
