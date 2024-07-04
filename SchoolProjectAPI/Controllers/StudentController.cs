using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolProjectAPI.Data;
using SchoolProjectAPI.Model;
using System.Data;

namespace SchoolProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly AppDbContext appDbContext;

        public StudentController(AppDbContext appDbContext)
        {
            this.appDbContext = appDbContext;    
        }

        
        [Route("GetAllStudents")]
        [HttpGet]
        [Authorize(Roles = "Admin, User")]
        public IActionResult GetAllStudents()
        {
            var getStudents = appDbContext.Students.ToList();
            return Ok(getStudents);
        }


        [Authorize(Roles = "Admin")]

        [Route("AddStudents")]
        [HttpPost]
        public IActionResult AddStudent(Student student)
        {
            try
            {
                appDbContext.Students.Add(student);
                var result = appDbContext.SaveChanges() > 0;
                if (result)
                {
                    return Ok(new { message = "data submitted successfully" });
                }
                return BadRequest(new { message = "operation failed" });
            }
            catch (Exception ex)
            {

                return BadRequest(new { message = "operation failed "+  ex.Message });
            }
          
        }

        [Authorize(Roles = "Admin")]

        [Route("UpdateStudents")]
        [HttpPut]
        public IActionResult UpdateStudent(Student student, int id)
        {
            var std = appDbContext.Students.Find(id);
            if(std == null)
            {
                return NotFound();
            }
            
            std.Name = student.Name;
            std.Email = student.Email;
            std.Semester = student.Semester;
            std.Address = student.Address;

            appDbContext.SaveChanges();
            return Ok(std);
        }

    }
}
