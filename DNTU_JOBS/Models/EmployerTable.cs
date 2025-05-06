using System;
using System.Collections.Generic;

namespace DNTU_JOBS.Models;

public partial class EmployerTable
{
    public int Id { get; set; }

    public string? ApplicationUserId { get; set; }

    public string? Name { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? Description { get; set; }

    public byte[]? Photo { get; set; }


    public string? FacebookLink { get; set; }
    public string? TwitterLink { get; set; }
    public string? LinkedInLink { get; set; }
    public string? InstagramLink { get; set; }


    public virtual ApplicationUser? ApplicationUser { get; set; }


    public virtual ICollection<JobTable> Jobs { get; set; } = new List<JobTable>();

}
