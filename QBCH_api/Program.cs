using Asp.Versioning;
using Cache_lib.Implementations;
using Cache_lib.Interfaces;
using CertManagement.Services.Implementations;
using CertManagement.Services.Interfaces;
using Crypto_lib.Service;
using KafkaService_lib.Services.Implementation;
using KafkaService_lib.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi.Models;
using QBCH_api.Services.Implementations;
using QBCH_api.Services.Implementations.V3;
using QBCH_api.Services.Interfaces;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Implementations;
using Qbch_db_lib.Services.Implementations.V3;
using Qbch_db_lib.Services.Interfaces;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.Configuration;
using QBCH_lib.Services.Implementations;
using QBCH_lib.Services.Implementations.V3;
using QBCH_lib.Services.Interfaces;
using QBCH_lib.Services.Interfaces.V3;
using QBCHService_lib.Services.Implementations;
using QBCHService_lib.Services.Implementations.V3;
using QBCHService_lib.Services.Interfaces;
using QBCHService_lib.Services.Interfaces.V3;
using Serilog;
using Serilog.Core;
using StackExchange.Redis;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using XmlService_lib.Services.Implementations.V3;
using XmlService_lib.Services.Interfaces.V3;
using XmlService_lib.Services.Implementations;
using XmlService_lib.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var serilog = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

builder.Services.Configure<ApiV3ContractOptions>(configuration.GetSection(ApiV3ContractOptions.SectionName));
var contractOptions = configuration.GetSection(ApiV3ContractOptions.SectionName).Get<ApiV3ContractOptions>() ?? new ApiV3ContractOptions();
var contractRules = new ApiV3ContractRules(contractOptions);

builder.Services.AddSingleton(contractOptions);
builder.Services.AddSingleton(contractRules);
builder.Host.UseSerilog(serilog);
builder.Services.AddControllers();

builder.Services.AddApiVersioning(options =>
{
    var isDefaultApiVersionSet = builder.Configuration.GetValue<bool>("Features:SetDefaultApiVersion");

    if (isDefaultApiVersionSet)
    {
        var DefaultApiVersion = builder.Configuration.GetValue<double>("Features:DefaultApiVersion");

        options.DefaultApiVersion = new ApiVersion(DefaultApiVersion);
        options.AssumeDefaultVersionWhenUnspecified = true;
    }

    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(), new HeaderApiVersionReader("api-version"));
})
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    }).AddMvc();

builder.Services.AddAuthentication(
             CertificateAuthenticationDefaults.AuthenticationScheme)
             .AddCertificate(options =>
             options.AllowedCertificateTypes = CertificateTypes.All);

// Добавление сервисов в контейнер

// Main
builder.Services.AddFeatureManagement(builder.Configuration.GetSection("FeatureFlags"));
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")));
builder.Services.AddTransient<ICryptoService, CryptoService>();
builder.Services.AddTransient<ICertManagementService, CertManagementService>();
builder.Services.AddSingleton<ICompressService, CompressService>();
ThreadPool.SetMinThreads(200, 200);
builder.Services.AddTransient<ICacheService, CacheService>();
builder.Services.AddSingleton<IBKIRequisitsHandler, BKIRequsits>();
builder.Services.AddSingleton<IKafkaService, KafkaService>();


// V_2.0
builder.Services.AddTransient<IXmlService, XmlService>();
builder.Services.AddTransient<IValidationService, ValidationService>();
builder.Services.AddTransient<IRepository, Repository>();
builder.Services.AddTransient<IQBCHService, QBCHService>();
builder.Services.AddTransient<ITransformer, Transformer>();
builder.Services.AddTransient<ITicketService, TicketService>();

// V_3.0
builder.Services.AddTransient<IXmlServiceV3, XmlServiceV3>();
builder.Services.AddTransient<IValidationServiceV3, ValidationServiceV3>();
builder.Services.AddTransient<IRepositoryV3, RepositoryV3>();
builder.Services.AddTransient<IQBCHServiceV3, QBCHServiceV3>();
builder.Services.AddTransient<ITicketServiceV3, TicketServiceV3>();
builder.Services.AddTransient<IDlPutServiceV3, DlPutServiceV3>();

// Добавление http-клиентов в HttpClientFactory
try
{
    var requireSignerCertificate = builder.Configuration.GetValue("Signer:RequireCertificate", true);
    X509Certificate2? signerCertificate = null;

    if (requireSignerCertificate)
    {
        var searchValue = builder.Configuration.GetValue<string>("Signer:SearchValue");
        var storeLocationValue = builder.Configuration.GetValue<string>("Signer:StoreLocation");
        var storeNameValue = builder.Configuration.GetValue<string>("Signer:StoreName");
        var findTypeValue = builder.Configuration.GetValue<string>("Signer:FindType");

        if (string.IsNullOrWhiteSpace(searchValue))
        {
            serilog.Error("Отсутствует значение для поиска сертификата для подписи запросов КБКИ.");
            return;
        }

        if (!Enum.TryParse<StoreLocation>(storeLocationValue, true, out var storeLocation))
        {
            storeLocation = StoreLocation.LocalMachine;
        }

        if (!Enum.TryParse<StoreName>(storeNameValue, true, out var storeName))
        {
            storeName = StoreName.My;
        }

        if (!Enum.TryParse<X509FindType>(findTypeValue, true, out var findType))
        {
            findType = X509FindType.FindByThumbprint;
        }

        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        signerCertificate = store.Certificates
            .Find(findType, searchValue, true)
            .FirstOrDefault();

        if (signerCertificate == null)
        {
            serilog.Error("Отсутствует сертификат для подписания запросов в КБКИ.");
            return;
        }
    }
    else
    {
        serilog.Warning("Проверка сертификата подписи отключена настройкой Signer:RequireCertificate=false.");
    }

    foreach (var item in builder.Configuration.GetSection("QBCH").GetChildren())
    {
        AddHttpClientToFactory(builder, serilog, signerCertificate, item, contractOptions.HttpClientTimeoutSeconds);
    }
}
catch (Exception ex)
{
    serilog.Fatal(ex, "В процессе добавления http-клиента возникла ошибка");
    return;
}

builder.Services.AddMediatR(opt =>
{
    opt.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    opt.Lifetime = ServiceLifetime.Transient;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v3", new OpenApiInfo { Title = "API сервиса QBCH V3", Version = "v3.0" });
    options.SwaggerDoc("v2", new OpenApiInfo { Title = "API сервиса QBCH V2", Version = "v2.0" });
    options.SwaggerDoc("v1.3", new OpenApiInfo { Title = "API сервиса QBCH V1", Version = "v1.3" });
    options.EnableAnnotations();
    options.UseInlineDefinitionsForEnums();
});

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCertificateForwarding();
app.UseDeveloperExceptionPage();

// Enable middleware to serve generated Swagger as a JSON endpoint.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1.3/swagger.json", "API сервиса QBCH V1");
    c.SwaggerEndpoint("v2/swagger.json", "API сервиса QBCH V2");
    c.SwaggerEndpoint("v3/swagger.json", "API сервиса QBCH V3");
});
app.MapControllers();
app.Run();

// Метод добавляющий http-client в фабрику клиентов
static void AddHttpClientToFactory(WebApplicationBuilder builder, Logger serilog, X509Certificate2? certificate, IConfigurationSection section, int httpClientTimeoutSeconds)
{
    var clientName = section.GetValue<string>("Name");
    var url = section.GetValue<string>("Url");
    var urlv2 = section.GetValue<string>("Urlv2");

    if (string.IsNullOrWhiteSpace(clientName))
    {
        serilog.Fatal("Отсутствует clientName для http-клиента КБКИ.");
        throw new NullReferenceException();
    }

    if (string.IsNullOrEmpty(url))
    {
        serilog.Fatal("Отсутствует BaseAddress для http-клиента {clientName}.", clientName);
        throw new NullReferenceException();
    }

    if (string.IsNullOrEmpty(urlv2))
    {
        serilog.Fatal("Отсутствует BaseAddress для http-клиента v3: {clientName}.", clientName);
        throw new NullReferenceException();
    }

    var httpClientBuilder = builder.Services.AddHttpClient(clientName, client =>
    {
        client.BaseAddress = new Uri(url);
        client.Timeout = TimeSpan.FromSeconds(httpClientTimeoutSeconds);
    });

    var httpClientBuilderV3 = builder.Services.AddHttpClient($"{clientName}v3", client =>
    {
        client.BaseAddress = new Uri(urlv2);
        client.Timeout = TimeSpan.FromSeconds(httpClientTimeoutSeconds);
    });

    if (certificate == null)
    {
        serilog.Warning("HttpClient {clientName} и {clientName}v3 зарегистрированы без клиентского сертификата.", clientName);
        return;
    }

    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ClientCertificates = { certificate },
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };
    });

    httpClientBuilderV3.ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ClientCertificates = { certificate },
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };
    });
}