using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;
using DeskGuardBackend.Enums;

namespace DeskGuardBackend.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<CompanyService> _logger;

        public CompanyService(
            DeskGuardDbContext dbContext,
            IAuditLogService auditLogService,
            ILogger<CompanyService> logger)
        {
            _dbContext = dbContext;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<Company> GetCompanyAsync(long id)
        {
            var company = await _dbContext.Companies.FindAsync(id);
            if (company == null) throw new KeyNotFoundException($"Company not found: {id}");
            return company;
        }

        public async Task<IEnumerable<Company>> GetCompaniesAsync()
        {
            return await _dbContext.Companies.ToListAsync();
        }

        public async Task<Company> CreateCompanyAsync(string name, string? email, string? phone)
        {
            try
            {
                var company = new Company
                {
                    Name = name,
                    Email = email,
                    Phone = phone,
                    IsActive = true
                };

                await _dbContext.Companies.AddAsync(company);
                await _dbContext.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    EventType.Create.ToString(),
                    $"Created company: {name}"
                );

                _logger.LogInformation("Company {CompanyName} created with ID {CompanyId}", name, company.Id);
                return company;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CompanyService::CreateCompanyAsync failed for {Name}", name);
                throw;
            }
        }
    }
}
