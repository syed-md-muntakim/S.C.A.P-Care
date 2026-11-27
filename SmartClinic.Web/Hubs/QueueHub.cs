using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartClinic.Web.Hubs;

/// <summary>
/// Server -> client only. After any queue change the server sends "QueueChanged" (doctor id, no personal data);
/// each page then re-fetches its own authorised HTML, so nothing private travels over the hub.
/// </summary>
[Authorize]
public class QueueHub : Hub { }
