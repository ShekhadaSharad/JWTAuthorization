﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharadDemoProject.DataContext;
using SharadDemoProject.Model.Employees;
using System.Security.Claims;
using Response = SharadDemoProject.Model.Authentication.Response;

namespace SharadDemoProject.Controllers
{
    public enum RoleTypes
    {
        Admin,
        Hr,
        Manager
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"Admin,Hr,Manager")]
    public class EmployeeController : ControllerBase
    {
        private readonly ApplicationContext _dbEmployee;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmployeeController(ApplicationContext context, IHttpContextAccessor httpContextAccessor)
        {
            _dbEmployee = context;
            _httpContextAccessor = httpContextAccessor;
        }

         [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeModel>>> GetEmployeeAsync()
        {
            var userName = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);
            try
            {
                if (_dbEmployee.Employees == null)
                {
                    return NotFound();
                }
                return await _dbEmployee.Employees.ToListAsync();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"HTTP : {Request.Method} : {Request.Path},{_httpContextAccessor?.HttpContext?.Request.Headers.UserAgent}, Ip :- {_httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress}  responded : {Response.StatusCode}. An error occurred: {ex.Message},login this user : {userName}");
                return BadRequest( new Response { Status = "Error", Message = "An error occurred while processing the request." });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeModel>> GetEmployeeAsync(int id)
        {
            var userName = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);
            try
            {
                if (_dbEmployee.Employees == null)
                {
                    return NotFound();
                }
                var employee = await _dbEmployee.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound(); 
                }
                return employee;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"HTTP : {Request.Method} : {Request.Path} responded : {Response.StatusCode}. An error occurred: {ex.Message},login this user : {userName}");
                return BadRequest(new Response { Status = "Error", Message = "An error occurred while processing the request." });
            }
        }

        [HttpPost("Create")]
        public async Task<ActionResult<EmployeeModel>> PostEmployeeAsync(EmployeeModel employeeDetails)
        {
            var userName = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);
            try
            {
                if (IsEmailAlreadyEntered(employeeDetails.EmpEmail))
                {
                    Serilog.Log.Warning($"Email ID: {employeeDetails.EmpEmail} already exists. login this user : {userName}");
                    return BadRequest("Email ID already exists.");
                }
                _dbEmployee.Employees.Add(employeeDetails);
                await _dbEmployee.SaveChangesAsync();
                return CreatedAtAction(null, null, new { id = employeeDetails.EmpId }, employeeDetails);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"HTTP : {Request.Method} : {Request.Path} responded : {Response.StatusCode}. An error occurred: {ex.Message},login this user : {userName}");
                return BadRequest(new Response { Status = "Error", Message = "An error occurred while processing the request." });
            }
        }
        private bool IsEmailAlreadyEntered(string email)
        {
            var existingEmployee = _dbEmployee.Employees.FirstOrDefault(e => e.EmpEmail.Equals(email));
            return existingEmployee != null;
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmployeeAsync(int id, EmployeeModel employeeDetails)
        {
            try
            {
                if (id != employeeDetails.EmpId)
                {
                    return NotFound();
                }
                _dbEmployee.Entry(employeeDetails).State = EntityState.Modified;
                try
                {
                    await _dbEmployee.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeAvilable(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                var userName = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);
                Serilog.Log.Error($"HTTP : {Request.Method} : {Request.Path} responded : {Response.StatusCode}. An error occurred: {ex.Message},login this user : {userName}");
                return BadRequest(new Response { Status = "Error", Message = "An error occurred while processing the request." });
            }
        }
        private bool EmployeeAvilable(int id)
        {
            return (_dbEmployee.Employees?.Any(x => x.EmpId == id)).GetValueOrDefault();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployeeAsync(int id)
        {
            try
            {
                if (_dbEmployee.Employees == null)
                {
                    return NotFound();
                }
                var employee = await _dbEmployee.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound();
                }
                _dbEmployee.Employees.Remove(employee);
                await _dbEmployee.SaveChangesAsync();

                return Ok("Employee Deleted Successfully");
            }
            catch (Exception ex)
            {
                var userName = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);
                Serilog.Log.Error($"HTTP : {Request.Method} : {Request.Path} responded : {Response.StatusCode}. An error occurred: {ex.Message},login this user : {userName}");
                return BadRequest(new Response { Status = "Error", Message = "An error occurred while processing the request." });
            }
        }
    }
}