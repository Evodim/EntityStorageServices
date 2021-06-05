using Microsoft.Extensions.DependencyInjection;
using System;

namespace EntityTableService.Extensions
{
    /// <summary>
    /// native DI helpers to inject EntityTableClient instance
    /// </summary>
    public static class ServiceCollectionExtensions
    {
      
        public static IServiceCollection AddEntityTableClient<T>(this IServiceCollection services,

       EntityTableClientOptions tableClientOptions,
       EntityTableConfig<T> tableClientConfig)
       where T : class, new()
        {
          return services.AddTransient<IEntityTableClient<T>>(_ => new EntityTableClient<T>(tableClientOptions, tableClientConfig));
        }

        public static IServiceCollection AddEntityTableClient<T>(this IServiceCollection services,          
            Action<EntityTableClientOptions> optionsAction,
            Action<EntityTableConfig<T>> configAction)
            where T : class, new()
        {
            return services.AddTransient(_ => EntityTableClient.Create(optionsAction, configAction));
        }
       
        public static IServiceCollection AddScopedEntityTableClient<T>(this IServiceCollection services,
         
           Action<EntityTableClientOptions> optionsAction,
           Action<EntityTableConfig<T>> configAction)
           where T : class, new()
        {
           
            return services.AddScoped(_ => EntityTableClient.Create(optionsAction, configAction));
        }
        public static IServiceCollection AddScopedEntityTableClient<T>(this IServiceCollection services,

          EntityTableClientOptions tableClientOptions,
          EntityTableConfig<T> tableClientConfig)
          where T : class, new()
        {

            return services.AddScoped<IEntityTableClient<T>>(_ => new EntityTableClient<T>(tableClientOptions, tableClientConfig));
        }
    }
}