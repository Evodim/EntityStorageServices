﻿using Microsoft.Extensions.DependencyInjection;
using System;

namespace EntityTableService.Extensions
{
    /// <summary>
    /// native DI helpers to inject EntityTableClient instance
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityTableClient<T>(this IServiceCollection services,
            string connectionString,
            Action<EntityTableClientOptions> tableClientOptions,
            Action<EntityTableConfig<T>> tableClientConfig)
            where T : class, new()
        {
            var options = new EntityTableClientOptions(connectionString, typeof(T).Name, maxConcurrentInsertionTasks: 1);
            var config = new EntityTableConfig<T>();

            tableClientOptions?.Invoke(options);
            tableClientConfig?.Invoke(config);

            return services.AddTransient<IEntityTableClient<T>>(_ => new EntityTableClient<T>(options, config));
        }

        public static IServiceCollection AddScopedEntityTableClient<T>(this IServiceCollection services,
           string connectionString,
           Action<EntityTableClientOptions> tableClientOptions,
           Action<EntityTableConfig<T>> tableClientConfig)
           where T : class, new()
        {
            var options = new EntityTableClientOptions(connectionString, typeof(T).Name, maxConcurrentInsertionTasks: 1);
            var config = new EntityTableConfig<T>();

            tableClientOptions?.Invoke(options);
            tableClientConfig?.Invoke(config);

            return services.AddScoped<IEntityTableClient<T>>(_ => new EntityTableClient<T>(options, config));
        }
    }
}