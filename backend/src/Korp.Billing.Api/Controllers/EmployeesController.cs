using Korp.Billing.Api.Contracts;
using Korp.Billing.Api.Data;
using Korp.Billing.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Korp.Billing.Api.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(
    BillingDbContext context
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Employee>>> GetAll(
        [FromQuery] bool includeInactive = true,
        CancellationToken cancellationToken = default
    )
    {
        var query = context.Employees
            .AsNoTracking()
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(employee => employee.IsActive);
        }

        var employees = await query
            .OrderBy(employee => employee.Name)
            .ToListAsync(cancellationToken);

        return Ok(employees);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Employee>> GetById(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var employee = await context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                currentEmployee => currentEmployee.Id == id,
                cancellationToken
            );

        if (employee is null)
        {
            return NotFound(new
            {
                message = "Funcionário não encontrado."
            });
        }

        return Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<Employee>> Create(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken
    )
    {
        var cpf = request.Cpf.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        var cpfAlreadyExists = await context.Employees
            .AnyAsync(
                employee => employee.Cpf == cpf,
                cancellationToken
            );

        if (cpfAlreadyExists)
        {
            return Conflict(new
            {
                message = "Já existe um funcionário com esse CPF."
            });
        }

        var emailAlreadyExists = await context.Employees
            .AnyAsync(
                employee => employee.Email == email,
                cancellationToken
            );

        if (emailAlreadyExists)
        {
            return Conflict(new
            {
                message = "Já existe um funcionário com esse e-mail."
            });
        }

        var employee = new Employee
        {
            Name = request.Name.Trim(),
            Cpf = cpf,
            Email = email,
            Phone = request.Phone.Trim(),
            Role = request.Role
        };

        context.Employees.Add(employee);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = employee.Id },
            employee
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Employee>> Update(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken
    )
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(
                currentEmployee => currentEmployee.Id == id,
                cancellationToken
            );

        if (employee is null)
        {
            return NotFound(new
            {
                message = "Funcionário não encontrado."
            });
        }

        var cpf = request.Cpf.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        var cpfAlreadyExists = await context.Employees
            .AnyAsync(
                currentEmployee =>
                    currentEmployee.Id != id &&
                    currentEmployee.Cpf == cpf,
                cancellationToken
            );

        if (cpfAlreadyExists)
        {
            return Conflict(new
            {
                message = "Já existe outro funcionário com esse CPF."
            });
        }

        var emailAlreadyExists = await context.Employees
            .AnyAsync(
                currentEmployee =>
                    currentEmployee.Id != id &&
                    currentEmployee.Email == email,
                cancellationToken
            );

        if (emailAlreadyExists)
        {
            return Conflict(new
            {
                message = "Já existe outro funcionário com esse e-mail."
            });
        }

        employee.Name = request.Name.Trim();
        employee.Cpf = cpf;
        employee.Email = email;
        employee.Phone = request.Phone.Trim();
        employee.Role = request.Role;
        employee.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Ok(employee);
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<ActionResult<Employee>> Deactivate(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(
                currentEmployee => currentEmployee.Id == id,
                cancellationToken
            );

        if (employee is null)
        {
            return NotFound(new
            {
                message = "Funcionário não encontrado."
            });
        }

        if (!employee.IsActive)
        {
            return Ok(employee);
        }

        employee.IsActive = false;
        employee.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Ok(employee);
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<ActionResult<Employee>> Activate(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(
                currentEmployee => currentEmployee.Id == id,
                cancellationToken
            );

        if (employee is null)
        {
            return NotFound(new
            {
                message = "Funcionário não encontrado."
            });
        }

        if (employee.IsActive)
        {
            return Ok(employee);
        }

        employee.IsActive = true;
        employee.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Ok(employee);
    }
}