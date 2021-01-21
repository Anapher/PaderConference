using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using FluentValidation.AspNetCore;
using JsonSubTypes;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using PaderConference.Auth;
using PaderConference.Core;
using PaderConference.Core.Domain.Entities;
using PaderConference.Core.Errors;
using PaderConference.Core.Services.Chat.Dto;
using PaderConference.Extensions;
using PaderConference.Infrastructure;
using PaderConference.Infrastructure.Auth;
using PaderConference.Infrastructure.Auth.AuthService;
using PaderConference.Infrastructure.Data;
using PaderConference.Infrastructure.Hubs;
using PaderConference.Infrastructure.Serialization;
using PaderConference.Services;
using StackExchange.Redis.Extensions.Core.Configuration;
using Swashbuckle.AspNetCore.Swagger;

namespace PaderConference
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        static Startup()
        {
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(IImmutableList<>),
                typeof(ImmutableListSerializer<>));
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(IImmutableDictionary<,>),
                typeof(ImmutableDictionarySerializer<,>));
            BsonSerializer.RegisterSerializer(new JTokenBsonSerializer());
            BsonSerializer.RegisterSerializer(new EnumSerializer<PermissionType>(BsonType.String));
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            // Register the ConfigurationBuilder instance of AuthSettings
            var authSettings = Configuration.GetSection(nameof(AuthSettings));
            services.Configure<AuthSettings>(authSettings);

            var optionsAuthService = Configuration.GetSection("OptionsAuthService");
            services.Configure<UserCredentialsOptions>(optionsAuthService);

            var signingKey =
                new SymmetricSecurityKey(Encoding.ASCII.GetBytes(authSettings[nameof(AuthSettings.SecretKey)]));

            // jwt wire up
            // Get options from app settings
            var jwtAppSettingOptions = Configuration.GetSection(nameof(JwtIssuerOptions));

            // Configure JwtIssuerOptions
            services.Configure<JwtIssuerOptions>(options =>
            {
                options.Issuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                options.Audience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)];
                options.SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            });

            // Configure MongoDb options
            services.Configure<MongoDbOptions>(Configuration.GetSection("MongoDb"));

            // add MongoDb
            //services.AddSingleton(services =>
            //    new MongoClient(services.GetRequiredService<MongoDbOptions>().ConnectionString));

            services.AddHostedService<MongoDbBuilder>();

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)],
                ValidateAudience = true,
                ValidAudience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                RequireExpirationTime = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(configureOptions =>
            {
                configureOptions.ClaimsIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                configureOptions.TokenValidationParameters = tokenValidationParameters;
                configureOptions.SaveToken = true;

                configureOptions.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    },
                };
            }).AddEquipmentAuth(options => { });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme,
                        EquipmentAuthExtensions.EquipmentAuthScheme).Build();
            });

            services.AddSignalR().AddNewtonsoftJsonProtocol(options =>
            {
                options.PayloadSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.PayloadSerializerSettings.Converters.Add(
                    new StringEnumConverter(new CamelCaseNamingStrategy()));

                options.PayloadSerializerSettings.Converters.Add(JsonSubtypesConverterBuilder
                    .Of<SendingMode>(nameof(SendingMode.Type)).RegisterSubtype<SendAnonymously>(SendAnonymously.TYPE)
                    .RegisterSubtype<SendPrivately>(SendPrivately.TYPE).SerializeDiscriminatorProperty().Build());
            });

            services.AddMvc().ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                    new BadRequestObjectResult(new FieldValidationError(context.ModelState
                        .Where(x => x.Value.ValidationState == ModelValidationState.Invalid)
                        .ToDictionary(x => x.Key, x => x.Value.Errors.First().ErrorMessage)));
            }).AddFluentValidation(fv =>
                fv.RegisterValidatorsFromAssemblyContaining<Startup>()
                    .RegisterValidatorsFromAssemblyContaining<CoreModule>()).AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                });

            services.AddAutoMapper(Assembly.GetExecutingAssembly(), typeof(CoreModule).Assembly);

            var redisConfig = Configuration.GetSection("Redis").Get<RedisConfiguration>() ?? new RedisConfiguration();
            services.AddStackExchangeRedisExtensions<CamelCaseNewtonSerializer>(redisConfig);

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "Pader Conference API", Version = "v1"});

                var scheme = new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please insert JWT with Bearer into field",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey
                };

                // Swagger 2.+ support
                c.AddSecurityDefinition("Bearer", scheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {{scheme, new List<string>()}});

                c.AddFluentValidationRules();
            });

            services.AddMediatR(typeof(Startup), typeof(CoreModule));

            services.AddHostedService<ConferenceInitializer>();

            // Now register our services with Autofac container.
            var builder = new ContainerBuilder();

            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new InfrastructureModule());
            builder.RegisterModule(new PresentationModule());

            builder.Populate(services);
            var container = builder.Build();
            // Create the IServiceProvider based on the container.
            return new AutofacServiceProvider(container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pader Conference API V1"));

            app.UseAuthentication();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller}/{action=Index}/{id?}");
                endpoints.MapHub<CoreHub>("/signalr");
            });
        }
    }
}