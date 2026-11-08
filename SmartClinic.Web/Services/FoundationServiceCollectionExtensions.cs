using SmartClinic.Web.Data.Foundation;

namespace SmartClinic.Web.Services.Foundation;

public static class FoundationServiceCollectionExtensions
{

    public static IServiceCollection AddFoundationFeatures(this IServiceCollection services)
    {
        services.AddScoped<IDoctorShiftRepository, DoctorShiftRepository>();
        services.AddScoped<IDoctorLeaveRepository, DoctorLeaveRepository>();
        services.AddScoped<IPatientProfileRepository, PatientProfileRepository>();

        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IPatientAccountService, PatientAccountService>();
        services.AddScoped<IDoctorAdminService, DoctorAdminService>();

        services.AddHostedService<FoundationDemoSeeder>();
        return services;
    }
}
