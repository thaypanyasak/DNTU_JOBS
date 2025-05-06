using DNTU_JOBS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace DNTU_JOBS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
        public DbSet<ApplicationUser> ApplicationUser { get; set; }
        public DbSet<UserTable> UserJobs { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<JobCategoryTable> JobCategories { get; set; }
        public DbSet<EmployerTable> Employers { get; set; }
        public DbSet<JobTable> Jobs { get; set; }


        public DbSet<District> Districts { get; set; }
        public DbSet<Ward> Wards { get; set; }
        public DbSet<FavoriteJob> FavoriteJobs { get; set; }

 
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatImage> ChatImages { get; set; }
        public DbSet<FileTable> Files { get; set; }



        public DbSet<Notification> Notifications { get; set; }

        public DbSet<AdminTable> Admins { get; set; }

        public DbSet<UserJobCategory> UserJobCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.ApplicationUser)
                .WithMany(u => u.Applications) 
                .HasForeignKey(ja => ja.ApplicationUserId)
                .OnDelete(DeleteBehavior.NoAction); 

            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.UserTable)
                .WithMany(ut => ut.Applications) // Use the correct property
                .HasForeignKey(ja => ja.UserTableId)
                .OnDelete(DeleteBehavior.SetNull); 


            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.Job)
                .WithMany(j => j.JobApplications)
                .HasForeignKey(ja => ja.JobId)
                .OnDelete(DeleteBehavior.Cascade); 

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FavoriteJob>()
                .HasOne(f => f.Job)
                .WithMany(j => j.FavoriteJobs)
                .HasForeignKey(f => f.JobId)
                .OnDelete(DeleteBehavior.NoAction);

            
            modelBuilder.Entity<FavoriteJob>()
                .HasOne(f => f.ApplicationUser)
                .WithMany(u => u.FavoriteJobs)
                .HasForeignKey(f => f.ApplicationUserId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatMessage>()
                        .HasOne(c => c.Receiver)
                        .WithMany()
                        .HasForeignKey(c => c.ReceiverId)
                        .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserJobCategory>()
            .HasKey(ujc => new { ujc.UserTableId, ujc.JobCategoryId });

            modelBuilder.Entity<UserJobCategory>()
                .HasOne(ujc => ujc.UserTable)
                .WithMany(u => u.UserJobCategories)
                .HasForeignKey(ujc => ujc.UserTableId);

            modelBuilder.Entity<UserJobCategory>()
                .HasOne(ujc => ujc.JobCategory)
                .WithMany(j => j.UserJobCategories)
                .HasForeignKey(ujc => ujc.JobCategoryId);

        }

    }
}
