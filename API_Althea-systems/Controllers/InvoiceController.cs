using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IInvoicePdfService _pdf;

    public InvoiceController(
        IInvoiceService invoiceService,
        IInvoiceRepository invoiceRepository,
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IInvoicePdfService pdf)
    {
        _invoiceService = invoiceService;
        _invoiceRepository = invoiceRepository;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _pdf = pdf;
    }

    /// <summary>
    /// Listing every invoice across the system is an admin-only operation.
    /// Customers should consume invoices either by id (with ownership check)
    /// or via a future /api/orders/{id}/invoices endpoint.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<ActionResult<PaginatedResponse<InvoiceDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        return Ok(await _invoiceService.GetAllAsync(page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id)
    {
        // Resolve the underlying order to find the owner; admins bypass.
        if (!HttpContext.IsCurrentUserAdmin())
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Invoice", id);
            var order = await _orderRepository.GetByIdAsync(invoice.OrderId)
                ?? throw new NotFoundException("Order", invoice.OrderId);
            HttpContext.RequireOwnershipOrAdmin(order.UserId);
        }
        return Ok(await _invoiceService.GetByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<ActionResult<InvoiceDto>> Create([FromBody] InvoiceCreateRequest request)
    {
        var invoice = await _invoiceService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
    }

    /// <summary>
    /// Phase 6: issues a credit note (avoir) against an existing paid invoice.
    /// Admin only + step-up gated — refunding money is the most sensitive
    /// invoicing op we have. Service enforces business invariants (Paid,
    /// Type=Invoice, amount ≤ remaining); the response carries the freshly
    /// created credit-note DTO so the front can immediately offer a download.
    /// </summary>
    [HttpPost("{id:guid}/credit-note")]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<ActionResult<InvoiceDto>> IssueCreditNote(
        Guid id,
        [FromBody] IssueCreditNoteRequest request,
        CancellationToken ct)
    {
        var creditNote = await _invoiceService.IssueCreditNoteAsync(id, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = creditNote.Id }, creditNote);
    }

    /// <summary>
    /// Downloads the invoice as a real PDF rendered server-side by QuestPDF.
    /// Same AuthZ semantics as GetById : admin OR owner of the underlying order.
    ///
    /// The PDF is generated on the fly (not cached) because it's cheap (~50 ms
    /// for a typical invoice) and we want it to always reflect the current
    /// status / data. If we ever need to freeze a PDF (legal archive), we'd
    /// snapshot the bytes at issuance time and store them in object storage.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Invoice", id);

        var order = await _orderRepository.GetByIdAsync(invoice.OrderId)
            ?? throw new NotFoundException("Order", invoice.OrderId);

        // Ownership : same gate as GetById. Admins bypass.
        HttpContext.RequireOwnershipOrAdmin(order.UserId);

        var user = await _userRepository.GetByIdAsync(order.UserId)
            ?? throw new NotFoundException("User", order.UserId);

        // The order's BillingAddress nav property may not be loaded depending
        // on the repository's includes — re-resolve from the user's addresses.
        var billing = user.Addresses.FirstOrDefault(a => a.Id == order.BillingAddressId)
            ?? throw new NotFoundException("BillingAddress", order.BillingAddressId);

        var bytes = _pdf.Render(invoice, order, user, billing);

        // Short id in the filename so the user gets something readable instead
        // of a 36-char guid. Same convention as the on-screen label.
        var filename = $"facture-{invoice.Id.ToString()[..8].ToUpperInvariant()}.pdf";
        return File(bytes, "application/pdf", filename);
    }
}
