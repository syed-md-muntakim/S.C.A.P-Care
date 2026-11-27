using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Common;
using SmartClinic.Web.Hubs;
using SmartClinic.Web.Models;
using SmartClinic.Web.Repositories;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Services;

public sealed record TokenResult(bool Success, string? Error = null, int? TokenNumber = null)
{
    public static TokenResult Ok(int? number = null) => new(true, null, number);
    public static TokenResult Fail(string error) => new(false, error);
}

public interface IQueueService
{
    Task<TokenResult> IssueTokenAsync(int appointmentId, string actorId, bool isStaff);
    Task<TokenResult> CallNextAsync(string doctorId);
    Task<TokenResult> CompleteAsync(int tokenId, string doctorId);
    Task<TokenResult> SkipAsync(int tokenId, string doctorId);
    Task<TokenResult> RequeueAsync(int tokenId, string doctorId);

    Task<DoctorQueueViewModel?> GetDoctorQueueAsync(string doctorId, bool canControl);
    Task<BoardViewModel> GetBoardAsync();
    Task<MyQueueViewModel> GetMyQueueAsync(string patientId);
    Task<int> CountWaitingTodayAsync(string? doctorId = null);
}

public class QueueService : IQueueService
{
    private const int MaxRetries = 3;
    private readonly IQueueRepository _tokens;
    private readonly IAppointmentRepository _appts;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IHubContext<QueueHub> _hub;

    public QueueService(IQueueRepository tokens, IAppointmentRepository appts,
        UserManager<ApplicationUser> users, IHubContext<QueueHub> hub)
    {
        _tokens = tokens; _appts = appts; _users = users; _hub = hub;
    }

    // ------------------------------------------------------------------ issue (check-in)
    public async Task<TokenResult> IssueTokenAsync(int appointmentId, string actorId, bool isStaff)
    {
        var appt = await _appts.GetAsync(appointmentId);
        if (appt is null) return TokenResult.Fail("Appointment not found.");
        if (!isStaff && appt.PatientId != actorId) return TokenResult.Fail("You can only get a token for your own appointment.");
        if (appt.Status != AppointmentStatus.Booked) return TokenResult.Fail("Only booked appointments can get a token.");
        if (appt.AppointmentDate != ClinicTime.Today) return TokenResult.Fail("A token can only be issued on the day of the appointment.");
        if (appt.Token is not null) return TokenResult.Fail($"Token #{appt.Token.TokenNumber} has already been issued for this appointment.");

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var number = await _tokens.NextNumberAsync(appt.DoctorId, appt.AppointmentDate);
            var token = new QueueToken
            {
                AppointmentId = appt.Id, DoctorId = appt.DoctorId, PatientId = appt.PatientId,
                QueueDate = appt.AppointmentDate, TokenNumber = number,
                Status = TokenStatus.Waiting, IssuedAt = ClinicTime.Now
            };
            _tokens.Add(token);
            try
            {
                await _tokens.SaveAsync();
                await NotifyAsync(appt.DoctorId);
                return TokenResult.Ok(number);
            }
            catch (DbUpdateException)
            {   // two check-ins at the same moment got the same number (or the same appointment): retry
                _tokens.Detach(token);
                if (await _tokens.ExistsForAppointmentAsync(appt.Id))
                    return TokenResult.Fail("A token has already been issued for this appointment.");
            }
        }
        return TokenResult.Fail("Could not issue a token right now. Please try again.");
    }

    // ------------------------------------------------------------------ doctor actions
    public async Task<TokenResult> CallNextAsync(string doctorId)
    {
        var today = ClinicTime.Today;
        if (await _tokens.HasInConsultationAsync(doctorId, today))
            return TokenResult.Fail("Complete or skip the current patient before calling the next one.");

        var next = await _tokens.NextWaitingAsync(doctorId, today);
        if (next is null) return TokenResult.Fail("No patients are waiting.");

        next.Status = TokenStatus.InConsultation;
        next.CalledAt = ClinicTime.Now;
        try { await _tokens.SaveAsync(); }
        catch (DbUpdateException) { return TokenResult.Fail("The queue just changed. Please try again."); }

        await NotifyAsync(doctorId);
        return TokenResult.Ok(next.TokenNumber);
    }

    public async Task<TokenResult> CompleteAsync(int tokenId, string doctorId)
    {
        var t = await _tokens.GetAsync(tokenId);
        if (t is null || t.DoctorId != doctorId) return TokenResult.Fail("Token not found.");
        if (t.Status != TokenStatus.InConsultation) return TokenResult.Fail("Only the patient currently in consultation can be completed.");

        t.Status = TokenStatus.Completed;
        t.CompletedAt = ClinicTime.Now;
        if (t.Appointment is not null) t.Appointment.Status = AppointmentStatus.Completed;
        await _tokens.SaveAsync();
        await NotifyAsync(doctorId);
        return TokenResult.Ok(t.TokenNumber);
    }

    public async Task<TokenResult> SkipAsync(int tokenId, string doctorId)
    {
        var t = await _tokens.GetAsync(tokenId);
        if (t is null || t.DoctorId != doctorId) return TokenResult.Fail("Token not found.");
        if (t.Status is not (TokenStatus.Waiting or TokenStatus.InConsultation)) return TokenResult.Fail("This token cannot be skipped.");

        t.Status = TokenStatus.Skipped;
        await _tokens.SaveAsync();
        await NotifyAsync(doctorId);
        return TokenResult.Ok(t.TokenNumber);
    }

    public async Task<TokenResult> RequeueAsync(int tokenId, string doctorId)
    {
        var t = await _tokens.GetAsync(tokenId);
        if (t is null || t.DoctorId != doctorId) return TokenResult.Fail("Token not found.");
        if (t.Status != TokenStatus.Skipped) return TokenResult.Fail("Only skipped tokens can return to the queue.");
        if (t.QueueDate != ClinicTime.Today) return TokenResult.Fail("This token belongs to a previous day.");

        t.Status = TokenStatus.Waiting;
        await _tokens.SaveAsync();
        await NotifyAsync(doctorId);
        return TokenResult.Ok(t.TokenNumber);
    }

    // ------------------------------------------------------------------ views
    public async Task<DoctorQueueViewModel?> GetDoctorQueueAsync(string doctorId, bool canControl)
    {
        var doctor = await _users.FindByIdAsync(doctorId);
        if (doctor is null || !await _users.IsInRoleAsync(doctor, Roles.Doctor)) return null;

        var today = ClinicTime.Today;
        var tokens = await _tokens.ForDoctorAsync(doctorId, today);
        var booked = await _appts.QueryAsync(new AppointmentFilter
        {
            DoctorId = doctorId, From = today, To = today, Status = AppointmentStatus.Booked
        });

        QueueEntry Map(QueueToken t) => new()
        {
            TokenId = t.Id, TokenNumber = t.TokenNumber, PatientName = t.Patient?.FullName ?? "Patient",
            AppointmentTime = t.Appointment is null ? "" : t.Appointment.StartTime.Hm(),
            Status = t.Status, IssuedAt = TimeOnly.FromDateTime(t.IssuedAt).Hm(),
            CalledAt = t.CalledAt is { } c ? TimeOnly.FromDateTime(c).Hm() : null
        };

        return new DoctorQueueViewModel
        {
            DoctorId = doctorId, DoctorName = NameFormat.Doctor(doctor.FullName), Specialty = doctor.Specialty,
            Date = today.Pretty(), CanControl = canControl, UpdatedAt = ClinicTime.TimeNow.Hm(),
            NowServing = tokens.Where(t => t.Status == TokenStatus.InConsultation).Select(Map).FirstOrDefault(),
            Waiting = tokens.Where(t => t.Status == TokenStatus.Waiting).Select(Map).ToList(),
            Skipped = tokens.Where(t => t.Status == TokenStatus.Skipped).Select(Map).ToList(),
            Completed = tokens.Where(t => t.Status == TokenStatus.Completed).Select(Map).ToList(),
            AwaitingCheckIn = booked.Where(a => a.Token is null).Select(a => new CheckInEntry
            {
                AppointmentId = a.Id, PatientName = a.Patient?.FullName ?? "Patient", AppointmentTime = a.StartTime.Hm()
            }).ToList()
        };
    }

    public async Task<BoardViewModel> GetBoardAsync()
    {
        var today = ClinicTime.Today;
        var tokens = await _tokens.ForDateAsync(today);
        var appts = await _appts.QueryAsync(new AppointmentFilter { From = today, To = today, ExcludeCancelled = true });

        var rows = appts.GroupBy(a => a.DoctorId).Select(g =>
        {
            var dt = tokens.Where(t => t.DoctorId == g.Key).ToList();
            var first = g.First();
            return new BoardRow
            {
                DoctorId = g.Key, DoctorName = NameFormat.Doctor(first.Doctor?.FullName ?? "Doctor"), Specialty = first.Doctor?.Specialty,
                AppointmentsToday = g.Count(), CompletedCount = dt.Count(t => t.Status == TokenStatus.Completed),
                NowServing = dt.FirstOrDefault(t => t.Status == TokenStatus.InConsultation)?.TokenNumber,
                Waiting = dt.Where(t => t.Status == TokenStatus.Waiting).OrderBy(t => t.TokenNumber).Select(t => t.TokenNumber).ToList()
            };
        }).OrderBy(r => r.DoctorName).ToList();

        return new BoardViewModel { Date = today.Pretty(), UpdatedAt = ClinicTime.TimeNow.Hm(), Rows = rows };
    }

    public async Task<MyQueueViewModel> GetMyQueueAsync(string patientId)
    {
        var today = ClinicTime.Today;
        var mine = await _appts.QueryAsync(new AppointmentFilter { PatientId = patientId, From = today, To = today, ExcludeCancelled = true });
        var rows = new List<MyQueueRow>();

        foreach (var a in mine)
        {
            var row = new MyQueueRow
            {
                AppointmentId = a.Id, DoctorName = NameFormat.Doctor(a.Doctor?.FullName ?? "Doctor"), Specialty = a.Doctor?.Specialty,
                AppointmentTime = a.StartTime.Hm(), TokenNumber = a.Token?.TokenNumber, Status = a.Token?.Status,
                CanCheckIn = a.Status == AppointmentStatus.Booked && a.Token is null
            };

            if (a.Token is { } mineToken)
            {
                var queue = await _tokens.ForDoctorAsync(a.DoctorId, today);
                var serving = queue.FirstOrDefault(t => t.Status == TokenStatus.InConsultation);
                row.NowServing = serving?.TokenNumber;
                if (mineToken.Status == TokenStatus.Waiting)
                {
                    row.PeopleAhead = queue.Count(t => t.Status == TokenStatus.Waiting && t.TokenNumber < mineToken.TokenNumber)
                                      + (serving is null ? 0 : 1);
                    var minutes = (a.EndTime.ToTimeSpan() - a.StartTime.ToTimeSpan()).TotalMinutes;
                    row.EstimatedWaitMinutes = (int)Math.Round(row.PeopleAhead * minutes);
                }
            }
            rows.Add(row);
        }
        return new MyQueueViewModel { Date = today.Pretty(), UpdatedAt = ClinicTime.TimeNow.Hm(), Rows = rows };
    }

    public Task<int> CountWaitingTodayAsync(string? doctorId = null) => _tokens.CountWaitingAsync(ClinicTime.Today, doctorId);

    // Tells every open queue page to refresh (only the doctor id is sent, never patient data).
    private Task NotifyAsync(string doctorId) => _hub.Clients.All.SendAsync("QueueChanged", doctorId);
}
