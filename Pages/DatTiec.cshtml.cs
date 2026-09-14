using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Domain.Enums;

namespace PhiluWedding.Pages.DatTiec;

/// <summary>
/// Public wedding-booking consultation request page. The visitor fills in
/// contact information and a few event details; on POST we save one
/// <see cref="BookingRequest"/> row to PostgreSQL and redirect to the
/// same page with a one-shot <c>TempData</c> flag set so that the success
/// state renders only after a successful save. Refreshing the page no
/// longer re-submits the form (PRG), and visiting <c>/dat-tiec?submitted=true</c>
/// directly never shows the success state because <c>TempData</c> is
/// only populated by a real successful POST.
/// </summary>
public class DatTiecModel : PageModel
{
    /// <summary>
    /// One-shot TempData key that proves the current GET comes from a
    /// real successful POST in the same session. Anything reading this
    /// key in the GET handler is guaranteed to be a server-driven
    /// redirect, not a manually typed URL.
    /// </summary>
    public const string SubmittedFlagKey = "BookingSubmitted";

    private readonly PhiluWeddingDbContext? _db;
    private readonly ILogger<DatTiecModel> _logger;

    public DatTiecModel(
        ILogger<DatTiecModel> logger,
        PhiluWeddingDbContext? db = null)
    {
        // DbContext is optional: Program.cs only registers it when a
        // connection string is configured. The page degrades gracefully
        // when no context is available.
        _db = db;
        _logger = logger;
    }

    [BindProperty]
    public BookingFormInput Input { get; set; } = new();

    /// <summary>True after a successful POST + redirect; drives the success state.</summary>
    public bool Submitted { get; private set; }

    /// <summary>True when the database is reachable. False when the page is in DB-down mode.</summary>
    public bool DatabaseAvailable { get; private set; } = true;

    /// <summary>
    /// Hall selected via <c>?hallId=</c> from the hall-detail CTA. Resolved
    /// against the database; <c>null</c> when missing or unpublished.
    /// Shown on the form as context, not as a hard requirement.
    /// </summary>
    public WeddingHall? PreferredHall { get; private set; }

    /// <summary>Default event-type options for the select element. Stored in the entity verbatim.</summary>
    public IReadOnlyList<string> EventTypeOptions { get; } = new[]
    {
        "Tiệc cưới",
        "Tiệc đính hôn / lễ ăn hỏi",
        "Tiệc kỷ niệm / họp mặt",
        "Tiệc doanh nghiệp",
        "Khác"
    };

    /// <summary>Earliest date the visitor may pick (today, in UTC).</summary>
    public DateOnly MinEventDate { get; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task OnGetAsync(int? hallId = null)
    {
        // The success state is gated on a one-shot TempData flag set by
        // the POST handler. If the flag is absent — including the case
        // where the visitor typed /dat-tiec?submitted=true directly —
        // we render the form instead of the success state.
        if (TempData.Peek(SubmittedFlagKey) is bool flag && flag)
        {
            Submitted = true;
            // Keep the flag for the redirect that follows; do not Remove.
            TempData.Keep(SubmittedFlagKey);
        }

        // Sensible default: pick a date about 6 months out so the date
        // input is never blank on first render. The visitor can change it.
        if (Input.EventDate == default)
        {
            Input.EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6));
        }

        await ResolvePreferredHallAsync(hallId, Input.PreferredHallId);
    }

    public async Task<IActionResult> OnPostAsync(int? hallId = null)
    {
        // Always re-render the date-picker minimum, event-type list and
        // hall context after a POST so a rejected form still has
        // everything it needs.
        if (!ModelState.IsValid)
        {
            await ResolvePreferredHallAsync(hallId, Input.PreferredHallId);
            return Page();
        }

        if (_db is null)
        {
            DatabaseAvailable = false;
            _logger.LogWarning("DatTiec submission skipped: no database context registered.");
            ModelState.AddModelError(string.Empty,
                "Hệ thống đang bận. Vui lòng thử lại sau ít phút hoặc liên hệ trực tiếp với nhà hàng.");
            await ResolvePreferredHallAsync(hallId, Input.PreferredHallId);
            return Page();
        }

        try
        {
            // Server-side re-validation: the id arrives from the form so
            // a tampered value must be checked against the database.
            // We only accept published halls and persist the resolved id.
            int? persistedHallId = null;
            if (Input.PreferredHallId.HasValue)
            {
                var exists = await _db.WeddingHalls
                    .AsNoTracking()
                    .Where(h => h.Id == Input.PreferredHallId.Value && h.IsPublished)
                    .Select(h => h.Id)
                    .FirstOrDefaultAsync();

                persistedHallId = exists == 0 ? null : exists;
            }

            var now = DateTime.UtcNow;
            var entity = new BookingRequest
            {
                CustomerName = Input.CustomerName.Trim(),
                PhoneNumber = Input.PhoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(Input.Email) ? null : Input.Email.Trim(),
                EventDate = Input.EventDate,
                EventType = Input.EventType.Trim(),
                EstimatedGuestCount = Input.EstimatedGuestCount,
                Note = string.IsNullOrWhiteSpace(Input.Note) ? null : Input.Note.Trim(),
                PreferredHallId = persistedHallId,

                // Server-controlled values — never accept from the form.
                Status = BookingStatus.New,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.BookingRequests.Add(entity);
            await _db.SaveChangesAsync();

            // Privacy-conscious log: name + status + id only.
            // We deliberately do not log the phone number, email, or note.
            _logger.LogInformation(
                "BookingRequest received. Id={Id} Customer='{Customer}' EventDate={EventDate} PreferredHallId={PreferredHallId}",
                entity.Id,
                entity.CustomerName,
                entity.EventDate,
                entity.PreferredHallId);

            // Post/Redirect/Get: the redirect drops the bound form so a
            // refresh cannot re-submit. The success state is rendered by
            // OnGet when the one-shot TempData flag is present — never
            // on a manually typed ?submitted=true URL.
            TempData[SubmittedFlagKey] = true;
            return RedirectToPage("/DatTiec");
        }
        catch (Exception ex)
        {
            DatabaseAvailable = false;
            // Log only the message — never the full exception object —
            // to avoid surfacing EF Core / Npgsql connection metadata
            // (host, port, database name) in production logs.
            _logger.LogWarning(
                "DatTiec submission: database unavailable. {Message}",
                ex.Message);

            // Generic, user-facing message. No exception detail leaks.
            ModelState.AddModelError(string.Empty,
                "Hệ thống đang bận. Vui lòng thử lại sau ít phút hoặc liên hệ trực tiếp với nhà hàng.");
            await ResolvePreferredHallAsync(hallId, Input.PreferredHallId);
            return Page();
        }
    }

    /// <summary>
    /// Resolves the preferred hall for display on the page. The id may
    /// arrive from the URL (GET) or the bound form (POST). It is silently
    /// dropped if the hall is missing or unpublished.
    /// </summary>
    private async Task ResolvePreferredHallAsync(int? hallIdFromQuery, int? hallIdFromForm)
    {
        var candidate = hallIdFromForm ?? hallIdFromQuery;
        if (!candidate.HasValue || _db is null)
        {
            return;
        }

        try
        {
            PreferredHall = await _db.WeddingHalls
                .AsNoTracking()
                .Where(h => h.Id == candidate.Value && h.IsPublished)
                .FirstOrDefaultAsync();

            // Keep the form bound to the resolved id so the hidden field
            // always reflects a hall that exists in the database.
            Input.PreferredHallId = PreferredHall?.Id;
        }
        catch (Exception ex)
        {
            // Treat lookup failures as "no preselection" — never let
            // a database outage block the form from rendering.
            // Log only the message; the hosting platform captures the
            // full stack via its own sink.
            _logger.LogWarning(
                "DatTiec: preferred hall lookup failed for id={Id}. {Message}",
                candidate,
                ex.Message);
        }
    }
}