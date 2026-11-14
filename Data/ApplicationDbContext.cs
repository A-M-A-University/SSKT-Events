using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SSKT_Events.Models;

namespace SSKT_Events.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Account Profiles
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Organizer> Organizers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }

        // Event Management
        public DbSet<EventRecord> Events { get; set; }
        public DbSet<EventPackage> EventPackages { get; set; }
        public DbSet<SupplierService> SupplierServices { get; set; }

        // Relationships
        public DbSet<OrganizerSupplier> OrganizerSuppliers { get; set; }
        public DbSet<PackageService> PackageServices { get; set; }

        // Bookings and Payments
        public DbSet<SupplierBooking> SupplierBookings { get; set; }
        public DbSet<EventPayment> EventPayments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Identity to Customer: One-to-One
            modelBuilder.Entity<Customer>()
                .HasOne(c => c.User)
                .WithOne()
                .HasForeignKey<Customer>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Identity to Organizer: One-to-One
            modelBuilder.Entity<Organizer>()
                .HasOne(o => o.User)
                .WithOne()
                .HasForeignKey<Organizer>(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Identity to Supplier: One-to-One
            modelBuilder.Entity<Supplier>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Supplier>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicate package-service combinations
            modelBuilder.Entity<PackageService>()
                .HasIndex(ps => new { ps.PackageId, ps.ServiceId })
                .IsUnique();

            // Prevent duplicate organizer-supplier partnerships
            modelBuilder.Entity<OrganizerSupplier>()
                .HasIndex(os => new { os.OrganizerId, os.SupplierId })
                .IsUnique();

            // Event relationships
            modelBuilder.Entity<EventRecord>()
                .HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplierBooking>()
                .HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventPayment>()
                .HasOne(p => p.Event)
                .WithMany(e => e.Payments)
                .HasForeignKey(p => p.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Supplier relationships
            modelBuilder.Entity<SupplierService>()
                .HasOne(s => s.Supplier)
                .WithMany()
                .HasForeignKey(s => s.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplierBooking>()
                .HasOne(b => b.Supplier)
                .WithMany()
                .HasForeignKey(b => b.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplierBooking>()
                .HasOne(b => b.Service)
                .WithMany()
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Organizer relationships
            modelBuilder.Entity<EventPackage>()
                .HasOne(p => p.Organizer)
                .WithMany()
                .HasForeignKey(p => p.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrganizerSupplier>()
                .HasOne(os => os.Organizer)
                .WithMany()
                .HasForeignKey(os => os.OrganizerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrganizerSupplier>()
                .HasOne(os => os.Supplier)
                .WithMany()
                .HasForeignKey(os => os.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            // Package relationships
            modelBuilder.Entity<PackageService>()
                .HasOne(ps => ps.Package)
                .WithMany(p => p.PackageServices)
                .HasForeignKey(ps => ps.PackageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PackageService>()
                .HasOne(ps => ps.Service)
                .WithMany()
                .HasForeignKey(ps => ps.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplierBooking>()
                .HasOne(b => b.Package)
                .WithMany()
                .HasForeignKey(b => b.PackageId)
                .OnDelete(DeleteBehavior.SetNull);

            // Optional payment-to-booking relationship
            modelBuilder.Entity<EventPayment>()
                .HasOne(p => p.Booking)
                .WithMany()
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.SetNull);

            // Decimal precision for financial values
            modelBuilder.Entity<EventRecord>()
                .Property(e => e.TotalBudget)
                .HasPrecision(18, 2);

            modelBuilder.Entity<EventPackage>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SupplierService>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SupplierBooking>()
                .Property(b => b.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<EventPayment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);
        }
    }
}