using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAPI.Data;
using StudentAPI.Models;

[ApiController]
[Route("api/[controller]")] // Base route for the controller
public class StudentController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public StudentController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/Student
    [Authorize]
    [HttpGet("list")]
    public async Task<IActionResult> GetAllStudents()
    {
        try{
            var students = await _db.Students
                .Include(s => s.Courses) // Include related courses
                .ToListAsync();
            return Ok(students);
        }catch(Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/Student/{id}
    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetStudentById(int id)
    {
        try
        {
            var student = await _db.Students
                .Include(s => s.Courses) // Include related courses
                .FirstOrDefaultAsync(s => s.Id == id);
            if (student == null)
            {
                return NotFound();
            }
            return Ok(student);
        }catch(Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/Student
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateStudent([FromBody] Student student)
    {
        try
        {
            // check model state
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // validate is this student already exists
            var existingStudent = await _db.Students
                .FirstOrDefaultAsync(s => s.Name == student.Name && s.Age == student.Age);
            if (existingStudent != null)
            {
                return Conflict("Student already exists.");
            }

            // Add student to database
            _db.Students.Add(student);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetStudentById), new { id = student.Id }, student);
        }catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/Student/{id}
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] Student updatedStudent)
    {
        try
        {
            if (id != updatedStudent.Id || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var student = await _db.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            student.Name = updatedStudent.Name;
            student.Age = updatedStudent.Age;
            // Update courses if necessary
            await _db.SaveChangesAsync();
            return NoContent();
        }catch(Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    
}