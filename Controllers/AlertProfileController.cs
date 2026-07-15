using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.DTOs.AlertThreshold;
using DeskGuardBackend.Exceptions;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/alert-profiles")]
    public class AlertProfileController : ControllerBase
    {
        private readonly IAlertProfileService _alertProfileService;
        private readonly ILogger<AlertProfileController> _logger;

        public AlertProfileController(IAlertProfileService alertProfileService, ILogger<AlertProfileController> logger)
        {
            _alertProfileService = alertProfileService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] AlertProfileFilterRequest filter)
        {
            try
            {
                var result = await _alertProfileService.GetAllAsync(filter);
                return Ok(ApiResponse<AlertProfileListResponse>.Ok(result, "Alert profiles retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list alert profiles");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve alert profiles."));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Show(long id)
        {
            try
            {
                var profile = await _alertProfileService.GetByIdAsync(id);
                return Ok(ApiResponse<AlertProfileDto>.Ok(profile, "Alert profile retrieved successfully."));
            }
            catch (AlertProfileException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message, ex.Errors.Count > 0 ? ex.Errors : null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get alert profile {ProfileId}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve alert profile."));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAlertProfileRequest request)
        {
            try
            {
                var profile = await _alertProfileService.CreateAsync(request);
                return StatusCode(201, ApiResponse<AlertProfileDto>.Ok(profile, "Alert profile created successfully."));
            }
            catch (AlertProfileException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message, ex.Errors.Count > 0 ? ex.Errors : null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create alert profile");
                return StatusCode(500, ApiResponse.Fail("Failed to create alert profile."));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateAlertProfileRequest request)
        {
            try
            {
                var profile = await _alertProfileService.UpdateAsync(id, request);
                return Ok(ApiResponse<AlertProfileDto>.Ok(profile, "Alert profile updated successfully."));
            }
            catch (AlertProfileException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message, ex.Errors.Count > 0 ? ex.Errors : null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update alert profile {ProfileId}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to update alert profile."));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _alertProfileService.DeleteAsync(id);
                return Ok(ApiResponse.Ok("Alert profile deleted successfully."));
            }
            catch (AlertProfileException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message, ex.Errors.Count > 0 ? ex.Errors : null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete alert profile {ProfileId}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to delete alert profile."));
            }
        }

        [HttpPost("{id}/duplicate")]
        public async Task<IActionResult> Duplicate(long id)
        {
            try
            {
                var profile = await _alertProfileService.DuplicateAsync(id);
                return Ok(ApiResponse<AlertProfileDto>.Ok(profile, "Alert profile duplicated successfully."));
            }
            catch (AlertProfileException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message, ex.Errors.Count > 0 ? ex.Errors : null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to duplicate alert profile {ProfileId}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to duplicate alert profile."));
            }
        }

        [HttpPost("{id}/companies")]
        public async Task<IActionResult> AssignToCompany(long id, [FromBody] AssignProfileToCompanyRequest request)
        {
            try
            {
                await _alertProfileService.AssignToCompanyAsync(id, request.CompanyId);
                return Ok(ApiResponse.Ok("Profile assigned to company successfully."));
            }
            catch (AlertProfileException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message, ex.Errors.Count > 0 ? ex.Errors : null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign profile {ProfileId} to company {CompanyId}", id, request.CompanyId);
                return StatusCode(500, ApiResponse.Fail("Failed to assign profile to company."));
            }
        }

        [HttpDelete("{id}/companies/{companyId}")]
        public async Task<IActionResult> UnassignFromCompany(long id, long companyId)
        {
            try
            {
                await _alertProfileService.UnassignFromCompanyAsync(id, companyId);
                return Ok(ApiResponse.Ok("Profile unassigned from company successfully."));
            }
            catch (AlertProfileException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message, ex.Errors.Count > 0 ? ex.Errors : null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unassign profile {ProfileId} from company {CompanyId}", id, companyId);
                return StatusCode(500, ApiResponse.Fail("Failed to unassign profile from company."));
            }
        }

        [HttpPost("{id}/machines")]
        public async Task<IActionResult> AssignToMachine(long id, [FromBody] AssignProfileToMachineRequest request)
        {
            try
            {
                await _alertProfileService.AssignToMachineAsync(id, request.MachineId);
                return Ok(ApiResponse.Ok("Profile assigned to machine successfully."));
            }
            catch (AlertProfileException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message, ex.Errors.Count > 0 ? ex.Errors : null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign profile {ProfileId} to machine {MachineId}", id, request.MachineId);
                return StatusCode(500, ApiResponse.Fail("Failed to assign profile to machine."));
            }
        }

        [HttpDelete("{id}/machines/{machineId}")]
        public async Task<IActionResult> UnassignFromMachine(long id, long machineId)
        {
            try
            {
                await _alertProfileService.UnassignFromMachineAsync(id, machineId);
                return Ok(ApiResponse.Ok("Profile unassigned from machine successfully."));
            }
            catch (AlertProfileException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message, ex.Errors.Count > 0 ? ex.Errors : null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unassign profile {ProfileId} from machine {MachineId}", id, machineId);
                return StatusCode(500, ApiResponse.Fail("Failed to unassign profile from machine."));
            }
        }
    }
}
