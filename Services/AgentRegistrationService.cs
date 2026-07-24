using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.DTOs.Machine;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Exceptions;
using DeskGuardBackend.Services.Interfaces;
using DeskGuardBackend.Enums;

namespace DeskGuardBackend.Services
{
    public class AgentRegistrationService : IAgentRegistrationService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AgentRegistrationService> _logger;

        public AgentRegistrationService(
            DeskGuardDbContext dbContext,
            IAuditLogService auditLogService,
            ILogger<AgentRegistrationService> logger)
        {
            _dbContext = dbContext;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<Machine> RegisterAsync(MachineRegistrationDto dto)
        {
            try
            {
                var user = await ValidateActivationTokenAsync(dto.ActivationToken);

                var existingMachine = await _dbContext.Machines
                    .FirstOrDefaultAsync(m => m.MachineUid == dto.MachineUid);

                if (existingMachine != null)
                {
                    throw new MachineRegistrationException($"A machine with UID \"{dto.MachineUid}\" is already registered.", 409);
                }

                using var transaction = await _dbContext.Database.BeginTransactionAsync();

                // Determine company and customer grouping
                long companyId = user.CompanyId ?? 1;
                long? customerId = null;

                var compName = (!string.IsNullOrWhiteSpace(dto.CompanyName) ? dto.CompanyName : "Default Enterprise").Trim();
                var mobNum = (!string.IsNullOrWhiteSpace(dto.MobileNumber) ? dto.MobileNumber : "").Trim();
                var custName = (!string.IsNullOrWhiteSpace(dto.CustomerName) ? dto.CustomerName : compName).Trim();

                var customer = await _dbContext.Customers
                    .FirstOrDefaultAsync(c => c.CompanyName.ToLower() == compName.ToLower() && c.MobileNumber == mobNum);

                if (customer == null)
                {
                    var totalCust = await _dbContext.Customers.CountAsync();
                    customer = new Customer
                    {
                        CustomerCode = $"CUST-{1001 + totalCust}",
                        CompanyName = compName,
                        CustomerName = custName,
                        MobileNumber = mobNum,
                        Email = dto.Email,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _dbContext.Customers.AddAsync(customer);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(custName)) customer.CustomerName = custName;
                    if (!string.IsNullOrWhiteSpace(dto.Email)) customer.Email = dto.Email;
                    customer.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                customerId = customer.Id;

                var machine = new Machine
                {
                    CompanyId = companyId,
                    CustomerId = customerId,
                    UserId = user.Id,
                    MachineUid = dto.MachineUid,
                    Hostname = dto.Hostname,
                    OperatingSystem = dto.OperatingSystem,
                    IsOnline = false,
                    IsActive = false,
                    EmployeeMobileNumber = dto.MobileNumber
                };

                await _dbContext.Machines.AddAsync(machine);
                await _dbContext.SaveChangesAsync();

                // Generate machine API token
                var apiToken = GenerateRandomString(64);
                var hashedToken = HashToken(apiToken);

                var machineToken = new MachineToken
                {
                    MachineId = machine.Id,
                    Token = hashedToken,
                    ExpiresAt = DateTime.UtcNow.AddYears(1)
                };

                await _dbContext.MachineTokens.AddAsync(machineToken);
                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                // Attach plain token to transient field for register endpoint response
                machine.ApiToken = apiToken;

                await _auditLogService.LogAsync(
                    EventType.Register.ToString(),
                    $"Machine registered: {machine.MachineUid} for company ID: {companyId}",
                    user: user,
                    machine: machine
                );

                _logger.LogInformation("Machine registered successfully: {MachineUid}", machine.MachineUid);
                return machine;
            }
            catch (MachineRegistrationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AgentRegistrationService::RegisterAsync failed for machine: {MachineUid}", dto.MachineUid);
                throw new MachineRegistrationException("Machine registration failed due to an internal error.", 500);
            }
        }

        public async Task<User> ValidateActivationTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new MachineRegistrationException("Activation token cannot be empty.", 422);
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.ActivationToken == token);

            if (user == null)
            {
                throw new MachineRegistrationException("Invalid or expired activation token.", 422);
            }

            if (!user.IsActive)
            {
                throw new MachineRegistrationException("The user associated with this activation token is inactive.", 403);
            }

            return user;
        }

        public string GenerateActivationToken()
        {
            return $"{GenerateRandomString(32)}-{GenerateRandomString(16)}";
        }

        public async Task<Machine> ActivateAsync(long machineId)
        {
            try
            {
                var machine = await _dbContext.Machines.FindAsync(machineId);

                if (machine == null)
                {
                    throw new MachineNotFoundException($"Machine with ID {machineId} not found.", 404);
                }

                machine.ActivatedAt = DateTime.UtcNow;
                machine.IsActive = true;

                await _dbContext.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    EventType.Update.ToString(),
                    $"Machine activated: {machine.MachineUid}",
                    machine: machine
                );

                _logger.LogInformation("Machine activated successfully: {MachineId}", machineId);
                return machine;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AgentRegistrationService::ActivateAsync failed for machine ID: {MachineId}", machineId);
                throw;
            }
        }

        private static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return string.Create(length, chars, (buffer, alphabet) =>
            {
                Span<byte> randomBytes = stackalloc byte[length];
                RandomNumberGenerator.Fill(randomBytes);
                for (int i = 0; i < length; i++)
                {
                    buffer[i] = alphabet[randomBytes[i] % alphabet.Length];
                }
            });
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
