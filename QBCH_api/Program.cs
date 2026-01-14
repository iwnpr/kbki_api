using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Adapters_lib;
using Asp.Versioning;
using Confluent.Kafka;
using Confluent.Kafka.Extensions.Diagnostics;
using Elastic.Apm.SerilogEnricher;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;
using Serilog.Enrichers.Span;
using Services_lib;

ThreadPool.SetMinThreads(200, 200);
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var serilog = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.WithElasticApmCorrelationInfo() //  trace.id  transaction.id
    .Enrich.WithSpan() //  span.id    
    .CreateLogger();

builder.Host.UseSerilog(serilog);
builder.Services.AddAllElasticApm();

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
builder.Services.AddFeatureManagement(builder.Configuration.GetSection("FeatureFlags"));
builder.Services
    .DIAddAdapters(configuration)
    .DIAddServices();
builder.Services.AddMediatR(opt =>
{
    opt.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    opt.Lifetime = ServiceLifetime.Transient;
});
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v2", new OpenApiInfo { Title = "API сервиса QBCH V2", Version = "v2.0" });
    options.EnableAnnotations();
    options.UseInlineDefinitionsForEnums();
});
// Добавление http-клиентов в HttpClientFactory
try
{
    var _searchValue = builder.Configuration.GetValue<string>("Signer:SearchValue");
    var _storeLocation = builder.Configuration.GetValue<string>("Signer:StoreLocation");
    var _storeName = builder.Configuration.GetValue<string>("Signer:StoreName");
    var _findType = builder.Configuration.GetValue<string>("Signer:FindType");

    if (string.IsNullOrWhiteSpace(_searchValue))
    {
        serilog.Error("Отсутствует значение для поиска сертифката для подписи запросов КБКИ.");
        return;
    }

    // Расположение хранилища сертифкатов
    if (!Enum.TryParse<StoreLocation>(_storeLocation, true, out var storeLocation))
        storeLocation = StoreLocation.LocalMachine;

    // Директория в хранилище
    if (!Enum.TryParse<StoreName>(_storeName, true, out var storeName))
        storeName = StoreName.My;

    // Определения параметра поиска
    if (!Enum.TryParse<X509FindType>(_findType, true, out var findType))
        findType = X509FindType.FindByThumbprint;

    X509Store store = new(storeName, storeLocation);
    store.Open(OpenFlags.ReadOnly);

    if (string.IsNullOrWhiteSpace(_searchValue))
    {
        throw new Exception("Сертификат не найден");
    }

    // Находим сертификаты с нужным именем и добавляем в коллекцию.
    var foundCertColl = store.Certificates.Find(findType, _searchValue, true).FirstOrDefault();

    // Сертификат не найден
    if (foundCertColl == null)
    {
        serilog.Error("Отсутствует сертификат для подписания запросов в КБКИ.");
        return;
    }

    // Добавление именованных http-клиентов в фабрику клиентов
    foreach (var item in builder.Configuration.GetSection("QBCH").GetChildren())
    {
        AddHttpClientToFactory(builder, serilog, foundCertColl, item);
    }
}
catch (Exception ex)
{
    serilog.Fatal(ex, "В процессе добавления http-клиента возникла ошибка");
    return;
}


var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCertificateForwarding();
app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v2/swagger.json", "API сервиса QBCH V2");
});

app.MapControllers();
app.Run();

// Метод добавляющий http-client в фабрику клиентов
static void AddHttpClientToFactory(WebApplicationBuilder builder, Logger serilog, X509Certificate2 certificate, IConfigurationSection section)
{
    var clientName = section.GetValue<string>("Name");
    var url = section.GetValue<string>("Url");
    var urlv2 = section.GetValue<string>("Urlv2");

    if (string.IsNullOrWhiteSpace(clientName))
    {
        serilog.Fatal("Отсутствует clientName для http-клиента КБКИ.");
        throw new NullReferenceException();
    }

    // Проверяем, что значение указано
    if (string.IsNullOrEmpty(url))
    {
        serilog.Fatal("Отсутствует BaseAddress для http-клиента {clientName}.", clientName);
        throw new NullReferenceException();
    }

    // Проверяем, что значение указано
    if (string.IsNullOrEmpty(urlv2))
    {
        serilog.Fatal("Отсутствует BaseAddress для http-клиента v2: {clientName}.", clientName);
        throw new NullReferenceException();
    }

    // Добавление http-client в фабрику HttpClientFactory
    builder.Services.AddHttpClient(clientName, client =>
    {
        client.BaseAddress = new(url);
        client.Timeout = TimeSpan.FromSeconds(4);
    })
        // Добавляем сертификат в запрос
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ClientCertificates = { certificate },
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
        });

    builder.Services.AddHttpClient($"{clientName}v2", client =>
    {
        client.BaseAddress = new(urlv2);
        client.Timeout = TimeSpan.FromSeconds(4);
    })
        // Добавляем сертификат в запрос
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ClientCertificates = { certificate },
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
        });
}