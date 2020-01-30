using System.Collections.Generic;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;

namespace ThAmCo.Auth
{
    public static class IdentityConfigurationExtensions
    {
        public static IEnumerable<IdentityResource> GetIdentityResources(this IConfiguration configuration)
        {
            return new IdentityResource[]
            {
                new IdentityResources.OpenId(),

                new IdentityResources.Profile(),

                new IdentityResource(name: "roles",
                                     displayName: "ThAmCo Application Roles",
                                     claimTypes: new [] { "role" })
            };
        }

        public static IEnumerable<ApiResource> GetIdentityApis(this IConfiguration configuration)
        {
            return new ApiResource[]
            {
                new ApiResource("thamco_account_api", "ThAmCo Account Management"),

				new ApiResource("thamco_orders_api", "ThAmCo Orders Service")
				{
					UserClaims = { "name", "role" }
				},

				new ApiResource("thamco_invoices_api", "ThAmCo Invoices Service")
			};
        }

        public static IEnumerable<Client> GetIdentityClients(this IConfiguration configuration)
        {
            return new Client[]
            {
				new Client
				{
					ClientId = "thamco_invoices_api",
					ClientName = "ThAmCo Invoice Service",

					AllowedGrantTypes = GrantTypes.ClientCredentials,

					ClientSecrets =
					{
						new Secret("secret".Sha256())
					},

					AllowedScopes =
					{
						"thamco_orders_api"
					},

					RequireConsent = false
				},
				new Client
				{
					ClientId = "thamco_orders_api",
					ClientName = "ThAmCo Orders Service",

					AllowedGrantTypes = GrantTypes.ClientCredentials,

					ClientSecrets =
					{
						new Secret("secret".Sha256())
					},

					AllowedScopes =
					{
						"thamco_invoices_api"
					},

					RequireConsent = false
				},
				
				new Client
				{
					ClientId = "thamco_home_app",
					ClientName = "ThAmCo Home App",

					AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,

					ClientSecrets =
					{
						new Secret("secret".Sha256())
					},

					AllowedScopes =
					{
						"thamco_account_api",

						"openid",
						"profile",
						"roles"
					},

					RequireConsent = false
				},

				new Client
				{
					ClientId = "thamco_orders_app",
					ClientName = "ThAmCo Orders App",

					AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,

					ClientSecrets =
					{
						new Secret("secret".Sha256())
					},

					AllowedScopes =
					{
						"openid",
						"profile",
						"roles"
					},

					RequireConsent = false
				}

			};
        }
    }
}
