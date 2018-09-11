using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IdentityServer4.Models;
using IdentityServer4;
using IdentityModel;
using IdentityServer4.Test;
using System.Security.Claims;

namespace OAuth2Gateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddIdentityServer()
                    .AddDeveloperSigningCredential()
                    .AddInMemoryClients(TestClients())
                    .AddInMemoryApiResources(GetApis())
                    .AddTestUsers(TestUsers);
            
            services.AddAuthentication("Bearer")
                    .AddIdentityServerAuthentication(options =>
                    {
                        options.Authority = "https://localhost:5001";
                        options.RequireHttpsMetadata = false;
                        
                        options.ApiName = "api1";
                    });
        }

        public List<TestUser> TestUsers = new List<TestUser> {
             new TestUser
            {
                SubjectId = "blah",
                Username = "dpetruse",
                Password = "password"
            }
        };

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseIdentityServer();
            app.UseAuthentication();
            //app.UseHttpsRedirection();
            app.UseMvc();
        }

        public IEnumerable<Client> TestClients(){
            yield return new Client
            {
                ClientId = "id",
                ClientSecrets = {
                    new Secret("secret".Sha256())
                },
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                AllowedScopes = {"api1"}
            };
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Email()
            };
        }

        public static IEnumerable<ApiResource> GetApis()
        {
            return new[]
            {
                // simple API with a single scope (in this case the scope name is the same as the api name)
                new ApiResource("api1", "Some API 1"),

                // expanded version if more control is needed
                new ApiResource
                {
                    Name = "api2",

                    // secret for using introspection endpoint
                    ApiSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    // include the following using claims in access token (in addition to subject id)
                    UserClaims = { JwtClaimTypes.Name, JwtClaimTypes.Email },

                    // this API defines two scopes
                    Scopes =
                    {
                        new Scope()
                        {
                            Name = "api2.full_access",
                            DisplayName = "Full access to API 2",
                        },
                        new Scope
                        {
                            Name = "api2.read_only",
                            DisplayName = "Read only access to API 2"
                        }
                    }
                }
            };
        }
    }
}
