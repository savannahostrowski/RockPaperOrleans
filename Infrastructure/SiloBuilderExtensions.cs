﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Clustering.CosmosDB;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Persistence.CosmosDB;
using RockPaperOrleans;
using System.Net;

namespace Microsoft.Extensions.Hosting
{
    public static class SiloBuilderExtensions
    {
        public static ISiloBuilder EnlistPlayer<TPlayer>(this ISiloBuilder builder) 
            where TPlayer : PlayerBase
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<TPlayer>();
                services.AddHostedService<PlayerWorkerBase<TPlayer>>();
            });

            return builder;
        }

        public static ISiloBuilder PlayRockPaperOrleans(this ISiloBuilder builder, IConfiguration configuration)
        {
            builder.CreateOrConnectToGameCluster(configuration)
                    .UseCosmosDbClustering()
                    .UseCosmosDbGrainStorage();

            return builder;
        }

        private static IAzureSiloBuilder CreateOrConnectToGameCluster(this ISiloBuilder builder, IConfiguration configuration)
        {
            builder
                .Configure<SiloOptions>(options =>
                {
                    options.SiloName = string.Format(configuration.GetValue<string>(
                        OrleansOnAzureConfiguration.EnvironmentVariableNames.SiloNameTemplate
                    ), Environment.MachineName);
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration.GetValue<string>(
                        OrleansOnAzureConfiguration.EnvironmentVariableNames.ClusterId
                    );
                    options.ServiceId = configuration.GetValue<string>(
                        OrleansOnAzureConfiguration.EnvironmentVariableNames.ServiceId
                    );
                })
                .Configure<EndpointOptions>(options =>
                {
                    options.AdvertisedIPAddress = IPAddress.Loopback;
                    options.SiloPort = configuration.GetValue<int>(
                        OrleansOnAzureConfiguration.EnvironmentVariableNames.SiloPort
                    );
                    options.GatewayPort = configuration.GetValue<int>(
                        OrleansOnAzureConfiguration.EnvironmentVariableNames.GatewayPort
                    );
                });

            IAzureSiloBuilder result = null;

            builder.ConfigureServices((ctx, services) =>
            {
                result = new AzureSiloBuilder(builder, ctx.Configuration);
                services.AddSingleton<IAzureSiloBuilder>(result);
            });

            return result;
        }
    }

    public interface IAzureSiloBuilder
    {
        IAzureSiloBuilder UseCosmosDbClustering();
        IAzureSiloBuilder UseCosmosDbGrainStorage();
    }

    public class AzureSiloBuilder : IAzureSiloBuilder
    {
        private ISiloBuilder SiloBuilder;
        private IConfiguration Configuration;

        public AzureSiloBuilder(ISiloBuilder builder, IConfiguration configuration)
        {
            SiloBuilder = builder;
            Configuration = configuration;
        }

        public IAzureSiloBuilder UseCosmosDbClustering()
        {
            SiloBuilder.UseCosmosDBMembership((CosmosDBClusteringOptions clusteringOptions) =>
            {
                clusteringOptions.ConnectionMode = Microsoft.Azure.Cosmos.ConnectionMode.Direct;
                clusteringOptions.AccountEndpoint = Configuration.GetValue<string>(
                    OrleansOnAzureConfiguration.EnvironmentVariableNames.CosmosDbAccountEndpoint
                    );
                clusteringOptions.AccountKey = Configuration.GetValue<string>(
                    OrleansOnAzureConfiguration.EnvironmentVariableNames.CosmosDbAccountKey
                    );
                clusteringOptions.DB = Configuration.GetValue<string>(
                    OrleansOnAzureConfiguration.EnvironmentVariableNames.CosmosDbDatabase
                    );
                clusteringOptions.CanCreateResources = true;
            });

            return this;
        }

        public IAzureSiloBuilder UseCosmosDbGrainStorage()
        {
            SiloBuilder.AddCosmosDBGrainStorageAsDefault((CosmosDBStorageOptions cosmosOptions) =>
            {
                cosmosOptions.AccountEndpoint = Configuration.GetValue<string>(
                    OrleansOnAzureConfiguration.EnvironmentVariableNames.CosmosDbAccountEndpoint
                    );
                cosmosOptions.AccountKey = Configuration.GetValue<string>(
                    OrleansOnAzureConfiguration.EnvironmentVariableNames.CosmosDbAccountKey
                    );
                cosmosOptions.DB = Configuration.GetValue<string>(
                    OrleansOnAzureConfiguration.EnvironmentVariableNames.CosmosDbDatabase
                    );
                cosmosOptions.CanCreateResources = true;
            });

            return this;
        }
    }
}