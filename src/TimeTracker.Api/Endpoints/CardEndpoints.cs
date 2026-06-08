using FastEndpoints;
using TimeTracker.Core;

namespace TimeTracker.Api;

/// <summary>POST /card/touch — register a card tap (arrival/departure). Double-tap ⇒ 409.</summary>
public sealed class TouchCardEndpoint(ITapService tap) : Endpoint<TouchCardRequest, CardUserResponse>
{
    public override void Configure()
    {
        Post("/card/touch");
        AllowAnonymous();
        Description(b => b
            .Produces<CardUserResponse>(200)
            .ProducesProblem(400).ProducesProblem(404).ProducesProblem(409));
        Summary(s => s.Summary = "Register a card tap (arrival, then departure). A third tap returns 409.");
    }

    public override async Task HandleAsync(TouchCardRequest req, CancellationToken ct)
    {
        var result = await tap.TouchAsync(req.CardUid, ct);
        await Send.OkAsync(new CardUserResponse(result.CardUid, result.UserId), ct);
    }
}

/// <summary>POST /card/assign — bind a card to a user.</summary>
public sealed class AssignCardEndpoint(ICardService cards) : Endpoint<AssignCardRequest, CardUserResponse>
{
    public override void Configure()
    {
        Post("/card/assign");
        AllowAnonymous();
        Description(b => b
            .Produces<CardUserResponse>(200)
            .ProducesProblem(400).ProducesProblem(404).ProducesProblem(409));
        Summary(s => s.Summary = "Bind a card to a user.");
    }

    public override async Task HandleAsync(AssignCardRequest req, CancellationToken ct)
    {
        var result = await cards.AssignAsync(req.UserId, req.CardUid, ct);
        await Send.OkAsync(new CardUserResponse(result.CardUid, result.UserId), ct);
    }
}

/// <summary>POST /card/delete — delete a card.</summary>
public sealed class DeleteCardEndpoint(ICardService cards) : Endpoint<DeleteCardRequest, CardUserResponse>
{
    public override void Configure()
    {
        Post("/card/delete");
        AllowAnonymous();
        Description(b => b
            .Produces<CardUserResponse>(200)
            .ProducesProblem(400).ProducesProblem(404));
        Summary(s => s.Summary = "Delete a card and return its former owner.");
    }

    public override async Task HandleAsync(DeleteCardRequest req, CancellationToken ct)
    {
        var result = await cards.DeleteAsync(req.CardUid, ct);
        await Send.OkAsync(new CardUserResponse(result.CardUid, result.UserId), ct);
    }
}

/// <summary>POST /card/list_by_user — list a user's cards.</summary>
public sealed class ListCardsByUserEndpoint(ICardService cards) : Endpoint<UserIdRequest, UserCardsResponse>
{
    public override void Configure()
    {
        Post("/card/list_by_user");
        AllowAnonymous();
        Description(b => b
            .Produces<UserCardsResponse>(200)
            .ProducesProblem(400).ProducesProblem(404));
        Summary(s => s.Summary = "List all cards bound to a user.");
    }

    public override async Task HandleAsync(UserIdRequest req, CancellationToken ct)
    {
        var cardUids = await cards.ListByUserAsync(req.UserId, ct);
        await Send.OkAsync(new UserCardsResponse(req.UserId, cardUids), ct);
    }
}

/// <summary>POST /card/delete_all_by_user — delete all of a user's cards.</summary>
public sealed class DeleteAllCardsByUserEndpoint(ICardService cards) : Endpoint<UserIdRequest, UserCardsResponse>
{
    public override void Configure()
    {
        Post("/card/delete_all_by_user");
        AllowAnonymous();
        Description(b => b
            .Produces<UserCardsResponse>(200)
            .ProducesProblem(400).ProducesProblem(404));
        Summary(s => s.Summary = "Delete all cards bound to a user and return the deleted UIDs.");
    }

    public override async Task HandleAsync(UserIdRequest req, CancellationToken ct)
    {
        var deleted = await cards.DeleteAllByUserAsync(req.UserId, ct);
        await Send.OkAsync(new UserCardsResponse(req.UserId, deleted), ct);
    }
}
