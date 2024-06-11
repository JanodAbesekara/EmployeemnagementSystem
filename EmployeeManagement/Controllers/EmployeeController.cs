using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EmployeeManagement.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
      
        private readonly IConfiguration _configuration;
        private readonly EmployeeContext _context;


        public EmployeeController(EmployeeContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("Register")]
        public async Task<ActionResult<Employee>> Register(EmployeeDTO request)
        {
            // Check if the user already exists
            if (await _context.Employee.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("User already exists");
            }

            // Hash the password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Map DTO to the Employee model
            var employee = new Employee
            {
                Name = request.Name,
                Department = request.Department,
                Email = request.Email,
                Phonenumber = request.Phonenumber,
                Password = passwordHash
            };

            // Add the employee to the database
            _context.Employee.Add(employee);
            await _context.SaveChangesAsync();

            return Ok(employee);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<Employee>> Login(EmployeeDTO request)
        {
            // Find the user by email
            var employee = await _context.Employee.FirstOrDefaultAsync(u => u.Email == request.Email);

            // Check if the user exists
            if (employee == null)
            {
                return BadRequest("Invalid credentials");
            }

            // Check if the password is correct
            if (!BCrypt.Net.BCrypt.Verify(request.Password, employee.Password))
            {
                return BadRequest("Invalid credentials");
            }

            // Generate JWT token
            string token = GenerateJwtToken(employee);


            return Ok("Token"+ token);
        }
        
        private string GenerateJwtToken(Employee employee)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
                new Claim(ClaimTypes.Email, employee.Email),
                new Claim(ClaimTypes.Name, employee.Name)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.Now.AddMinutes(30),
             signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }



    }
}
