using Microsoft.AspNetCore.Mvc.Rendering;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.ViewModels;

public class QueueEntry
{
    public int TokenId { get; set; }
    public int TokenNumber { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string AppointmentTime { get; set; } = string.Empty;
    public TokenStatus Status { get; set; }
    public string IssuedAt { get; set; } = string.Empty;
    public string? CalledAt { get; set; }
}

public class CheckInEntry
{
    public int AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string AppointmentTime { get; set; } = string.Empty;
}

public class DoctorQueueViewModel
{
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string Date { get; set; } = string.Empty;
    public bool CanControl { get; set; }          // doctor: call / complete / skip
    public QueueEntry? NowServing { get; set; }
    public List<QueueEntry> Waiting { get; set; } = new();
    public List<QueueEntry> Skipped { get; set; } = new();
    public List<QueueEntry> Completed { get; set; } = new();
    public List<CheckInEntry> AwaitingCheckIn { get; set; } = new();
    public string UpdatedAt { get; set; } = string.Empty;
}

public class QueueIndexViewModel
{
    public bool IsAdmin { get; set; }
    public string? SelectedDoctorId { get; set; }
    public List<SelectListItem> Doctors { get; set; } = new();
    public DoctorQueueViewModel? Queue { get; set; }
}

public class BoardRow
{
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public int AppointmentsToday { get; set; }
    public int CompletedCount { get; set; }
    public int? NowServing { get; set; }
    public List<int> Waiting { get; set; } = new();
}

public class BoardViewModel
{
    public string Date { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public List<BoardRow> Rows { get; set; } = new();
}

public class MyQueueRow
{
    public int AppointmentId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string AppointmentTime { get; set; } = string.Empty;
    public int? TokenNumber { get; set; }
    public TokenStatus? Status { get; set; }
    public int? NowServing { get; set; }
    public int PeopleAhead { get; set; }
    public int EstimatedWaitMinutes { get; set; }
    public bool CanCheckIn { get; set; }
}

public class MyQueueViewModel
{
    public string Date { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public List<MyQueueRow> Rows { get; set; } = new();
}
