namespace SmartClinic.Web.Models;

public class AdminDashboardViewModel
{
    public int ApprovedDoctors { get; set; }
    public int TotalPatients { get; set; }
    public List<ApplicationUser> PendingDoctors { get; set; } = new();
    
    public int TotalMedicines { get; set; }
    public int LowStockMedicines { get; set; }
    public int OutOfStockMedicines { get; set; }
    public int ExpiringSoonMedicines { get; set; }
    public int ExpiredMedicines { get; set; }

}
