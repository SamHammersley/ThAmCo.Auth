using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThAmCo.Auth.Data.Account;

namespace ThAmCo.Auth
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // configure EF context to use for storing Identity account data
            services.AddDbContext<AccountDbContext>(options => options.UseSqlServer(
                Configuration.GetConnectionString("AccountConnection"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", "account")
            ));

            // configure Identity account management
            services.AddIdentity<AppUser, AppRole>()
                    .AddEntityFrameworkStores<AccountDbContext>()
                    .AddDefaultTokenProviders();

			// add bespoke factory to translate our AppUser into claims
			services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, AppClaimsPrincipalFactory>();

            // configure Identity security options
            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 6;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;

                // Sign-in settings
                options.SignIn.RequireConfirmedEmail = false;
            });

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

			string azureStorageUri = Environment.GetEnvironmentVariable("JWT_BEARER_AUTHORITY");
			services.AddAuthentication()
                    .AddJwtBearer("thamco_account_api", options =>
                    {
                        options.Audience = "thamco_account_api";
                        options.Authority = azureStorageUri;
                    });

            // configure IdentityServer (provides OpenId Connect and OAuth2)
			var connectionString = Configuration.GetConnectionString("GrantsConnection");
			var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

			services.AddIdentityServer()
					.AddConfigurationStore(options =>
					{
						options.ConfigureDbContext = b =>
							b.UseSqlServer(connectionString,
								sql => sql.MigrationsAssembly(migrationsAssembly));
					})
					.AddOperationalStore(options =>
					{
						options.ConfigureDbContext = b =>
							b.UseSqlServer(connectionString,
								sql => sql.MigrationsAssembly(migrationsAssembly));
					})
					.AddAspNetIdentity<AppUser>()
					.AddDeveloperSigningCredential(persistKey: false);
			// TODO: developer signing cert above should be replaced with a real one

			Configuration.GetIdentityResources();
		}

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();

			InitializeDatabase(app);
			// use IdentityServer middleware during HTTP requests
			app.UseIdentityServer();
        }

		private void InitializeDatabase(IApplicationBuilder app)
		{
			using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
			{
				serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

				var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
				context.Database.Migrate();
				if (!context.Clients.Any())
				{
					foreach (var client in Configuration.GetIdentityClients())
					{
						context.Clients.Add(client.ToEntity());
					}
					context.SaveChanges();
				}

				if (!context.IdentityResources.Any())
				{
					foreach (var resource in Configuration.GetIdentityResources())
					{
						context.IdentityResources.Add(resource.ToEntity());
					}
					context.SaveChanges();
				}

				if (!context.ApiResources.Any())
				{
					foreach (var resource in Configuration.GetIdentityApis())
					{
						context.ApiResources.Add(resource.ToEntity());
					}
					context.SaveChanges();
				}
			}
		}

	}
}
