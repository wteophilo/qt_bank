using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.Text;
using MediatR;
using FluentValidation;
using QtBank.Api.Application.Behaviors;
using QtBank.Api.Application.Accounts.Commands;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Repositories;
using QtBank.Api.Domain.Models;
using QtBank.Api.Infrastructure.Repositories;
using QtBank.Api.Infrastructure.Messaging;
using QtBank.Api.Infrastructure.Security;
using QtBank.Api.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Builder;
using QtBank.Api.Infrastructure.Endpoints.v1;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "QtBank API",
        Description = "A secure REST API for banking operations, transactions, and account management.",
        Contact = new OpenApiContact
        {
            Name = "QtBank Engineering Team",
            Email = "support@qtbank.com"
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});


// Register FluentValidation validators
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Configure MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// Register Domain and Infrastructure dependencies
builder.Services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();
builder.Services.AddSingleton<ITransactionRepository, InMemoryTransactionRepository>();
builder.Services.AddSingleton<IPubSubPublisher, InMemoryPubSubPublisher>();

// Configure JWT Authentication
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
        ValidIssuer = TokenGenerator.Issuer,
        ValidAudience = TokenGenerator.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TokenGenerator.Secret))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseValidationExceptionMiddleware();

// Seed mock data for testing in Swagger UI
using (var scope = app.Services.CreateScope())
{
    var accountRepo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

    var aliceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    accountRepo.SaveAsync(new Account
    {
        Id = aliceId,
        AccountNumber = "111111",
        Balance = 5000.00m,
        OwnerName = "Alice Smith",
        CreatedAt = DateTime.UtcNow.AddMonths(-1),
        Status = AccountStatus.Active
    }).GetAwaiter().GetResult();

    var bobId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    accountRepo.SaveAsync(new Account
    {
        Id = bobId,
        AccountNumber = "222222",
        Balance = 150.50m,
        OwnerName = "Bob Johnson",
        CreatedAt = DateTime.UtcNow.AddDays(-10),
        Status = AccountStatus.Active
    }).GetAwaiter().GetResult();

    var charlieId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    accountRepo.SaveAsync(new Account
    {
        Id = charlieId,
        AccountNumber = "333333",
        Balance = 0.00m,
        OwnerName = "Charlie Davis",
        CreatedAt = DateTime.UtcNow.AddDays(-2),
        Status = AccountStatus.Inactive
    }).GetAwaiter().GetResult();
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapTokenEndpoints();
app.MapAccountEndpoints();
app.MapTransactionEndpoints();

app.Run();

public partial class Program { }