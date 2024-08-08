namespace cyberforgepc
{
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using cyberforgepc.BusinessLogic;
    using cyberforgepc.Database.Context;
    using cyberforgepc.Domain.Repository;
    using cyberforgepc.Domain.UnitOfWork;
    using cyberforgepc.Helpers.Authentication;
    using cyberforgepc.Helpers.Mail;
    using cyberforgepc.Helpers.Security;
    using cyberforgepc.Helpers.Settings;
    using cyberforgepc.Helpers.Storage;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi.Models;
    using Quartz;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;

    public class Startup
    {
        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public Startup(IHostEnvironment env, ILogger<Startup> logger)
        {
            var builder = new ConfigurationBuilder()
                        .SetBasePath(env.ContentRootPath)
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                        .AddEnvironmentVariables();

            if (env.IsDevelopment())
                builder.AddUserSecrets<Startup>();


            Configuration = builder.Build();

            _logger = logger;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.SetIsOriginAllowed(origin => true)
                        .AllowAnyMethod().AllowAnyHeader().AllowCredentials()
                );
            });

            services.AddOptions();

            services.AddSingleton<BlobStorageService>();

            var connectionString = Configuration.GetSection("ConnectionStrings");
            var appSettings = Configuration.GetSection("AppSettings");
            var emailSetting = Configuration.GetSection("Email");

            services.Configure<ConnectionStrings>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.Configure<EmailSetting>(Configuration.GetSection("EmailSetting"));

            var _connectionStrings = connectionString.Get<ConnectionStrings>();
            var _appSettings = appSettings.Get<AppSettings>();

            // Add configuration values
            _logger.LogInformation("Added configuration values.");

            services.AddRouting(options => options.LowercaseUrls = true);

            // Add connection string database
            _logger.LogInformation("Added connection string.");

            services.AddEntityFrameworkSqlServer().AddDbContext<CyberforgepcContext>(
                options => options.UseSqlServer(_connectionStrings.Connection,
                msSqlServerOptions => msSqlServerOptions.CommandTimeout(60))
            );
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Set the JWT
            _logger.LogInformation("Set the JWT configuration.");

            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            services.AddControllers();

            // Add Swagger Documentation
            _logger.LogInformation("Added Swagger documentation configuration.");

            services.AddSwaggerGen(c =>
            {
                        c.SwaggerDoc("v1",
                    new OpenApiInfo
                    {
                        Title = "cyberforgepc Doc - V1",
                        Version = "v1"
                    }
                 );

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            services.AddEndpointsApiExplorer();

            // Adding DI with Autofac
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterType<AuthenticationHelper>().As<IAuthenticationHelper>();
            containerBuilder.RegisterType<MailHelper>().As<IMailHelper>();
            containerBuilder.RegisterType<SecurityHelper>().As<ISecurityHelper>();

            containerBuilder.RegisterGeneric(typeof(Repository<>)).As(typeof(IRepository<>));

            containerBuilder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerRequest();

            containerBuilder.RegisterType<Products>().As<IProducts>();
            containerBuilder.RegisterType<Users>().As<IUsers>();
            containerBuilder.RegisterType<Coupons>().As<ICoupons>();
            containerBuilder.RegisterType<WishLists>().As<IWishLists>();
            containerBuilder.RegisterType<Categories>().As<ICategories>();
            containerBuilder.RegisterType<Orders>().As<IOrders>();
            containerBuilder.RegisterType<Layout>().As<ILayout>();

            containerBuilder.RegisterType<InventoryTransactions>().As<IInventoryTransactions>();

            containerBuilder.RegisterType<UnitOfWork>().As<IUnitOfWork>().PropertiesAutowired();

            containerBuilder.Populate(services);

            var container = containerBuilder.Build();
            return container.Resolve<IServiceProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                _logger.LogInformation("In Development environment.");
                app.UseDeveloperExceptionPage();
            }
            else
            {
                _logger.LogInformation($"In {env.EnvironmentName} environment.");
                app.UseHsts();
            }

            _logger.LogInformation("Enable Swagger and make endpoint UI.");


            app.UseHttpsRedirection();
            app.UseRouting();
            // Enable CORS so the Vue client can send requests
            _logger.LogInformation("Enable CORS Origin.");
            app.UseCors("CorsPolicy");
            _logger.LogInformation("Use auth, redirection and MVC.");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSwagger();

            app.UseSwaggerUI();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


        }

    }
}
