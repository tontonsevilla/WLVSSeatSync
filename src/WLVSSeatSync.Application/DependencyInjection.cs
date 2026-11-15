using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using WLVSSeatSync.Application.Behaviors;
using WLVSSeatSync.Application.Common;

namespace WLVSSeatSync.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);           // finds every handler
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));   // wraps every handler
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<SeatMapUpdates>();

        return services;
    }
}
