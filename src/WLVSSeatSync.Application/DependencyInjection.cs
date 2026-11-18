using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using WLVSSeatSync.Application.Behaviors;
using WLVSSeatSync.Application.Common;
using WLVSSeatSync.Application.Orders;

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
            config.AddOpenBehavior(typeof(AuditBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<SeatMapUpdates>();
        services.AddScoped<OrderPaidMessageFactory>();

        return services;
    }
}
