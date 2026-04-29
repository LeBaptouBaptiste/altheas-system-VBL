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

    public InvoiceController(
        IInvoiceService invoiceService,
        IInvoiceRepository invoiceRepository,
        IOrderRepository orderRepository)
    {
        _invoiceService = invoiceService;
        _invoiceRepository = invoiceRepository;
        _orderRepository = orderRepository;
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
}
