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
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Repositories;
using QtBank.Api.Domain.Models;
using QtBank.Api.Infrastructure.Repositories;
using QtBank.Api.Infrastructure.Messaging;
using QtBank.Api.Infrastructure.Security;
using QtBank.Api.Infrastructure.Middlewares;
using QtBank.Api.Infrastructure;
using QtBank.Api.Infrastructure.Http;
using QtBank.Api.Infrastructure.Telemetry;
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

// Configure Correlation ID Infrastructure
builder.Services.AddCorrelationIdServices();

// Configure Distributed Tracing with OpenTelemetry
builder.Services.AddTelemetryServices("QtBank.Api");

// Example registration of an external HttpClient using the CorrelationIdDelegatingHandler
builder.Services.AddHttpClient("ExternalMicroservice", client =>
{
    client.BaseAddress = new Uri("https://api.externalmicroservice.local");
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

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

// Enable Correlation ID Middleware early in the pipeline to enrich logging and capture incoming IDs
app.UseCorrelationId();

app.UseValidationExceptionMiddleware();

// Seed mock data for testing in Swagger UI
app.SeedMockData();

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