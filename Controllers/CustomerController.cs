using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.DTOs.Machine;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IAgentRegistrationService _registrationService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            ICustomerService customerService,
            IAgentRegistrationService registrationService,
            ILogger<CustomerController> logger)
        {
            _customerService = customerService;
            _registrationService = registrationService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/customers
        /// Returns customer groups with aggregate system metrics, search filtering, sorting, and pagination.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCustomers(
            [FromQuery] string? search,
            [FromQuery] string? sortBy = "company_asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var (items, totalCount) = await _customerService.GetCustomersAsync(search, sortBy, page, pageSize);

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return Ok(new
                {
                    data = items,
                    total = totalCount,
                    page = page,
                    pageSize = pageSize,
                    lastPage = totalPages > 0 ? totalPages : 1
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get customer groups.");
                return StatusCode(500, new { message = "An internal error occurred while fetching customer groups." });
            }
        }

        /// <summary>
        /// GET /api/customers/{id}
        /// Returns customer details and aggregate system status summary.
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCustomerById(long id)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    return NotFound(new { message = $"Customer with ID {id} not found." });
                }

                return Ok(new { data = customer });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get customer details for ID: {Id}", id);
                return StatusCode(500, new { message = "An internal error occurred while fetching customer details." });
            }
        }

        /// <summary>
        /// GET /api/customers/{id}/machines
        /// Returns list of machines registered under the specified customer.
        /// </summary>
        [HttpGet("{id}/machines")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCustomerMachines(long id)
        {
            try
            {
                var machines = await _customerService.GetCustomerMachinesAsync(id);
                return Ok(new { data = machines });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get machines for customer ID: {Id}", id);
                return StatusCode(500, new { message = "An internal error occurred while fetching customer machines." });
            }
        }

        /// <summary>
        /// POST /api/customer/register
        /// Endpoint for registering an agent under a customer/company.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterCustomerAgent([FromBody] MachineRegistrationDto dto)
        {
            try
            {
                var machine = await _registrationService.RegisterAsync(dto);
                return Ok(new
                {
                    message = "Agent successfully registered to customer group.",
                    machineId = machine.Id,
                    machineUid = machine.MachineUid,
                    apiToken = machine.ApiToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Customer agent registration failed for UID: {MachineUid}", dto.MachineUid);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
