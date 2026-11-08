namespace SmartClinic.Web.Models;

public class AdminDashboardViewModel
{
    public int ApprovedDoctors { get; set; }
    public int TotalPatients { get; set; }
    public List<ApplicationUser> PendingDoctors { get; set; } = new();
}
