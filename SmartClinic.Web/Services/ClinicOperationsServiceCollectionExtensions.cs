using SmartClinic.Web.Repositories;

namespace SmartClinic.Web.Services;

/// <summary>Registers Sprint 1 #20 (Bed &amp; Cabin) and Sprint 2 #2 / #4 (Appointment Booking, Queue &amp; Token).</summary>
public static class ClinicOperationsServiceCollectionExtensions
{
    public static IServiceCollection AddClinicOperations(this IServiceCollection services)
    {
        services.AddScoped<IBedRepository, BedRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IQueueRepository, QueueRepository>();

        services.AddScoped<IBedService, BedService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IQueueService, QueueService>();

        services.AddSignalR();
        return services;
    }
}
