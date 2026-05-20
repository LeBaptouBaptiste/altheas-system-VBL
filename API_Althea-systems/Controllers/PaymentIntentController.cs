using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Payments;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

/// <summary>
/// Stripe PaymentIntent lifecycle. The frontend calls POST /api/payments/intents
/// once it's on the payment step of /checkout with a valid (Pending) order;
/// the response carries the client_secret consumed by stripe.confirmPayment().
///
/// The actual order confirmation happens asynchronously via the webhook
/// handler (commit 13) — this endpoint only creates the intent.
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentIntentController : ControllerBase
{
    private readonly IStripeService _stripe;
    private readonly IOrderRepository _orders;
    private readonly IUserRepository _users;

    public PaymentIntentController(
        IStripeService stripe,
        IOrderRepository orders,
        IUserRepository users)
    {
        _stripe = stripe;
        _orders = orders;
        _users = users;
    }

    [HttpPost("intents")]
    public async Task<ActionResult<CreatePaymentIntentResponse>> CreateIntent(
        [FromBody] CreatePaymentIntentRequest request,
        CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(request.OrderId)
            ?? throw new NotFoundException("Order", request.OrderId);

        // Ownership: a user can only pay for their own orders. Admins
        // are explicitly excluded here — an admin acting on a customer's
        // behalf should use a dedicated workflow, not this endpoint.
        var currentUserId = HttpContext.GetCurrentUserId()
            ?? throw new ForbiddenException("Authentication required.");
        if (order.UserId != currentUserId)
            throw new ForbiddenException("This order does not belong to you.");

        if (order.PaymentMethod != PaymentMethod.Card)
        {
            throw new AppValidationException(
                $"Order {order.Id} uses {order.PaymentMethod} — only Card orders can be paid via Stripe.");
        }

        if (order.PaymentStatus == PaymentStatus.Validated)
        {
            throw new AppValidationException(
                $"Order {order.Id} is already paid.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new AppValidationException(
                $"Order {order.Id} is cancelled.");
        }

        var user = await _users.GetByIdAsync(currentUserId)
            ?? throw new NotFoundException("User", currentUserId);

        // Phase 7: validate + persist store credit application BEFORE
        // creating the PaymentIntent. This lets the front toggle the
        // "use my credit" checkbox AFTER reaching the payment step and
        // simply re-call this endpoint — the order's CreditAppliedCents
        // stays in sync and StripeService recomputes the reduced amount.
        if (request.CreditAppliedCents < 0)
        {
            throw new AppValidationException("Credit applied cannot be negative.");
        }
        if (request.CreditAppliedCents > user.CreditBalanceCents)
        {
            throw new AppValidationException(
                $"Credit applied ({request.CreditAppliedCents} cents) exceeds " +
                $"available balance ({user.CreditBalanceCents} cents).");
        }
        order.CreditAppliedCents = request.CreditAppliedCents;

        // Create (or replace) the PaymentIntent. If a previous intent exists
        // for this order (failed retry, abandoned session), we don't cancel
        // it explicitly — Stripe will auto-expire it after 24h. Persisting
        // the new ID overwrites the old one so the webhook can still match.
        // StripeService deducts order.CreditAppliedCents from the Stripe
        // amount and throws if the remainder is below Stripe's 0.50 € EUR
        // minimum — we let that surface as a 500 since validation here
        // would duplicate the TTC math.
        var intent = await _stripe.CreatePaymentIntentAsync(order, user, request.SaveCard, ct);

        order.StripePaymentIntentId = intent.Id;
        order.StripePaymentStatus = intent.Status;
        order.UpdatedAt = DateTime.UtcNow;
        await _orders.UpdateAsync(order);

        return Ok(new CreatePaymentIntentResponse(
            ClientSecret: intent.ClientSecret,
            PaymentIntentId: intent.Id,
            Amount: intent.Amount,
            Currency: intent.Currency));
    }
}
