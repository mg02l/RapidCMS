﻿using System;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using RapidCMS.Common.Authorization;
using RapidCMS.Common.Data;
using RapidCMS.Common.Helpers;
using RapidCMS.Common.Models;
using RapidCMS.Common.Models.Config;
using RapidCMS.Common.Services;
using RapidCMS.Common.ValueMappers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RapidCMSMiddleware
    {
        public static IServiceCollection AddRapidCMS(this IServiceCollection services, Action<CmsConfig>? config = null)
        {
            var rootConfig = new CmsConfig();
            config?.Invoke(rootConfig);

            if (rootConfig.AllowAnonymousUsage)
            {
                services.AddSingleton<IAuthorizationHandler, AllowAllAuthorizationHandler>();
            }

            services.AddSingleton(rootConfig);

            services.AddScoped<Root>();

            services.AddTransient<ICollectionService, CollectionService>();
            services.AddTransient<ITreeService, TreeService>();
            services.AddTransient<IUIService, UIService>();

            services.AddScoped<IExceptionHelper, ExceptionHelper>();

            services.AddSingleton<DefaultValueMapper>();

            services.AddSingleton<BoolValueMapper>();
            services.AddSingleton<NullableBoolValueMapper>();
            services.AddSingleton<IntValueMapper>();
            services.AddSingleton<NullableIntValueMapper>();
            services.AddSingleton<LongValueMapper>();
            services.AddSingleton<NullableLongValueMapper>();
            services.AddSingleton<FloatValueMapper>();
            services.AddSingleton<NullableFloatValueMapper>();

            services.AddSingleton(typeof(CollectionValueMapper<>), typeof(CollectionValueMapper<>));
            services.AddSingleton(typeof(EnumValueMapper<>), typeof(EnumValueMapper<>));

            services.AddSingleton(typeof(EnumDataProvider<>), typeof(EnumDataProvider<>));

            services.AddHttpContextAccessor();
            services.AddScoped<HttpContextAccessor>();

            services.AddHttpClient();
            services.AddScoped<HttpClient>();

            return services;
        }

        public static IApplicationBuilder UseRapidCMS(this IApplicationBuilder app)
        {
            return app;
        }
    }
}
