using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DNTU_JOBS.Models;

public partial class JobCategoryTable
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Category Name is required.")]
    [StringLength(100, ErrorMessage = "Category Name must be less than 100 characters.")]
    public string? CategoryName { get; set; }

    [StringLength(500, ErrorMessage = "Description must be less than 500 characters.")]
    public string? Description { get; set; }

    public virtual ICollection<UserTable> EmployeeTables { get; set; } = new List<UserTable>();
    
    public virtual ICollection<JobTable>? Jobs { get; set; }

    public virtual ICollection<UserJobCategory> UserJobCategories { get; set; } = new List<UserJobCategory>();
}
