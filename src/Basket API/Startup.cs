using Basket.API.Configuration.Constants;
using Basket.API.Data;
using Basket.API.Data.Interfaces;
using Basket.API.Filters;
using Basket.API.Services;
using Basket.API.Services.Interfaces;
using CorrelationId;
using CorrelationId.DependencyInjection;
using CorrelationId.HttpClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net;
using System.Text.Json.Serialization;

namespace Basket.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllHeaders",
                      builder =>
                      {
                          builder.AllowAnyMethod()
                                 .AllowAnyHeader()
                                 .AllowCredentials();
                      });
            });

            services
                .AddControllers(options =>
                {
                    options.InputFormatters.RemoveType<XmlDataContractSerializerInputFormatter>();
                    options.InputFormatters.RemoveType<XmlSerializerInputFormatter>();

                    options.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();
                    options.OutputFormatters.RemoveType<StreamOutputFormatter>();
                    options.OutputFormatters.RemoveType<StringOutputFormatter>();
                    options.OutputFormatters.RemoveType<XmlDataContractSerializerOutputFormatter>();
                    options.OutputFormatters.RemoveType<XmlSerializerOutputFormatter>();

                    options.Filters.Add<ValidateModelStateFilter>();
                    options.Filters.Add<GlobalExceptionFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            services.AddHealthChecks();

            //CorrelationId
            services.AddDefaultCorrelationId(options =>
            {
                options.AddToLoggingScope = true;
                options.RequestHeader = "X-Correlation-Id";
                options.ResponseHeader = "X-Correlation-Id";
                options.UpdateTraceIdentifier = false;
            });

            //Compression
            services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
            services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
            services.AddResponseCompression();

            //MongoDb DataContext
            var mongoClientSettings = MongoClientSettings.FromConnectionString(Configuration.GetConnectionString("MongoDB"));
            mongoClientSettings.ReadConcern = ReadConcern.Majority;
            mongoClientSettings.ReadPreference = ReadPreference.SecondaryPreferred;
            mongoClientSettings.WriteConcern = WriteConcern.WMajority;
            services.AddSingleton<IMongoClient>(new MongoClient(mongoClientSettings));
            services.AddSingleton<IMongoDbDataContext, MongoDbDataContext>();

            //RedisCache DataContext
            var redisCacheConfig = Configuration.GetSection("RedisCache");
            string redisEndpoint = $"{redisCacheConfig.GetValue<string>("Endpoint")}:{redisCacheConfig.GetValue<int>("Port")},password={redisCacheConfig.GetValue<string>("Password")}";

            services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = ConfigurationOptions.Parse(redisEndpoint);

                options.ConfigurationOptions.AbortOnConnectFail = false;
                options.ConfigurationOptions.ConnectRetry = 5;
                options.ConfigurationOptions.DefaultDatabase = redisCacheConfig.GetValue<int>("Database");
                options.ConfigurationOptions.KeepAlive = 300;
                options.ConfigurationOptions.ConnectTimeout = 25000;
                options.ConfigurationOptions.Ssl = redisCacheConfig.GetValue<bool>("UseSsl");
                options.ConfigurationOptions.ReconnectRetryPolicy = new LinearRetry(100);
            });
            services.AddSingleton<IRedisCacheDataContext, RedisCacheDataContext>();

            //HttpClientFactory
            services.AddHttpClient<IHttpClientDataContext, HttpClientDataContext>(c =>
            {
                c.BaseAddress = new Uri(Configuration.GetSection("DummyHttpClient")["BaseAddress"]);
                c.DefaultRequestHeaders.Add("Accept", "application/json");
                c.DefaultRequestHeaders.Connection.Add("Keep-Alive");
            }).AddCorrelationIdForwarding();

            //Services
            services.AddSingleton<IBasketService, BasketService>();

            //Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = ApiConfigurationConsts.ApiName, Version = ApiConfigurationConsts.ApiVersionV1, Description = ApiConfigurationConsts.ApiName });
                c.EnableAnnotations();
                c.DocumentFilter<TagDescriptionsDocumentFilter>();
                c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();
                c.OperationFilter<Filters.SecurityRequirementsOperationFilter>();
                c.OperationFilter<CorrelationIdOperationFilter>();
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                        },
                        new List<string>()
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCorrelationId();

            app.UseHealthChecks("/health");

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c =>
            {
#if !DEBUG
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
                var basePath = "/api-basket"; // <-- nginx ingress path of values.yaml
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.Servers = new System.Collections.Generic.List<OpenApiServer>
                {
                    new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}{basePath}" }
                });
#endif
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("./swagger/v1/swagger.json", $"{ApiConfigurationConsts.ApiName} {ApiConfigurationConsts.ApiVersionV1}");
                c.RoutePrefix = string.Empty;
                c.DocumentTitle = ApiConfigurationConsts.ApiName;
                c.EnableFilter();
                c.DefaultModelsExpandDepth(-1); // Hide models in Swagger UI
                c.DisplayRequestDuration();

                c.OAuthAppName(ApiConfigurationConsts.ApiName);
            });

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseResponseCompression();

            app.UseCors("AllowAllHeaders");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
