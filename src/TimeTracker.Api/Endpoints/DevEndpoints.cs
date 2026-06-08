using System.ComponentModel.DataAnnotations;
using FastEndpoints;
using TimeTracker.Core;

namespace TimeTracker.Api;

// ─────────────────────────────────────────────────────────────────────────────
// DEMO-ONLY, NON-SPEC dev endpoints. These exist *solely* so TimeTracker.Demo can
// bootstrap data the 13 spec endpoints cannot produce on their own:
//   • /dev/create_user — there is no create-user endpoint; assign/set throw 404 for
//     an unknown user_id, so a user row must be inserted before anything else works.
//   • /dev/seed_tap    — /card/touch is stamped with TimeProvider "now" (today only);
//     this records a tap at an explicit instant so the demo can build multi-day history.
//
// Both are gated by <see cref="DevGate"/> (Development env, or EnableSeedEndpoints=true)
// and return 404 when disabled — which the demo reads as "seeding off, fall back to live
// taps". If the demo were dropped, this whole file would be deleted with it.
//
// They deliberately reach for Core's repository interfaces directly (not a Core service):
// the concession stays fenced in the Api and never pollutes Core's unit-tested surface.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Decides whether the demo-only /dev endpoints are active.</summary>
internal static class DevGate
{
    public static bool Enabled(IHostEnvironment env, IConfiguration config) =>
        env.IsDevelopment() || config.GetValue("EnableSeedEndpoints", defaultValue: false);
}

// create_user: { user_id, display_name? } → { user_id, created }
public sealed record CreateUserRequest(
    [property: Range(1, long.MaxValue)] long UserId,
    string? DisplayName = null);

public sealed record CreateUserResponse(long UserId, bool Created);

// seed_tap: { card_uid, at, intent? } → { card_uid, user_id }
// `intent` is accepted for forward-compat but ignored: arrival vs departure is inferred
// from that day's existing taps, exactly like /card/touch.
public sealed record SeedTapRequest(
    [property: Required, MaxLength(64)] string CardUid,
    DateTimeOffset At,
    string? Intent = null);

/// <summary>POST /dev/create_user — insert a user row (idempotent). Demo-only, env-gated.</summary>
public sealed class CreateUserEndpoint(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IHostEnvironment env,
    IConfiguration config) : Endpoint<CreateUserRequest, CreateUserResponse>
{
    public override void Configure()
    {
        Post("/dev/create_user");
        AllowAnonymous();
        Description(b => b.Produces<CreateUserResponse>(200).ProducesProblem(400).ProducesProblem(404));
        Summary(s => s.Summary = "DEMO-ONLY: seed a user row (there is no spec create-user endpoint).");
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        if (!DevGate.Enabled(env, config))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var created = false;
        if (!await users.ExistsAsync(req.UserId, ct))
        {
            // Fully qualified: the FastEndpoints base class has its own `User` (ClaimsPrincipal) member.
            users.Add(TimeTracker.Core.User.Create(req.UserId, req.DisplayName));
            await unitOfWork.SaveChangesAsync(ct);
            created = true;
        }

        await Send.OkAsync(new CreateUserResponse(req.UserId, created), ct);
    }
}

/// <summary>POST /dev/seed_tap — record a tap at an explicit instant. Demo-only, env-gated.</summary>
public sealed class SeedTapEndpoint(
    ICardRepository cards,
    IUserRepository users,
    IWorkDayRepository workDays,
    IUnitOfWork unitOfWork,
    IEventPublisher publisher,
    TimeProvider clock,
    IHostEnvironment env,
    IConfiguration config) : Endpoint<SeedTapRequest, CardUserResponse>
{
    public override void Configure()
    {
        Post("/dev/seed_tap");
        AllowAnonymous();
        Description(b => b
            .Produces<CardUserResponse>(200)
            .ProducesProblem(400).ProducesProblem(404).ProducesProblem(409).ProducesProblem(422));
        Summary(s => s.Summary = "DEMO-ONLY: record a tap at an explicit instant to backfill multi-day history.");
    }

    public override async Task HandleAsync(SeedTapRequest req, CancellationToken ct)
    {
        if (!DevGate.Enabled(env, config))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Mirrors TapService.TouchAsync, but stamps the tap at the supplied instant instead of "now".
        var normalized = CardUidNormalizer.Normalize(req.CardUid);
        var card = await cards.FindByUidAsync(normalized, ct)
            ?? throw new CardNotFoundException(normalized);

        if (!await users.ExistsAsync(card.UserId, ct))
        {
            throw new UserNotFoundException(card.UserId);
        }

        // Calendar day in the system's local frame (same as TapService's clock.GetLocalNow()).
        var localAt = TimeZoneInfo.ConvertTime(req.At, clock.LocalTimeZone);
        var date = DateOnly.FromDateTime(localAt.DateTime);
        var instant = req.At.ToUniversalTime();

        var workDay = await workDays.FindByUserAndDateAsync(card.UserId, date, ct);

        TapKind kind;
        if (workDay is null)
        {
            workDay = WorkDay.Open(card.UserId, date, instant);
            workDays.Add(workDay);
            kind = TapKind.Arrival;
        }
        else
        {
            kind = workDay.RecordTap(instant); // AlreadyTapped → 409, departure < arrival → 422
        }

        await unitOfWork.SaveChangesAsync(ct);
        await publisher.PublishAsync(new CardTappedEvent(card.Uid, card.UserId, kind, instant), ct);

        await Send.OkAsync(new CardUserResponse(card.Uid, card.UserId), ct);
    }
}
