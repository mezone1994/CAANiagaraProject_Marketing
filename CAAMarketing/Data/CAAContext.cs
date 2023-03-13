using CAAMarketing.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CAAMarketing.ViewModels;

namespace CAAMarketing.Data
{
    public class CAAContext : DbContext
    {
        //To give access to IHttpContextAccessor for Audit Data with IAuditable
        private readonly IHttpContextAccessor _httpContextAccessor;

        //Property to hold the UserName value
        public string UserName
        {
            get; private set;
        }

        public CAAContext(DbContextOptions<CAAContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            if (_httpContextAccessor.HttpContext != null)
            {
                //We have a HttpContext, but there might not be anyone Authenticated
                UserName = _httpContextAccessor.HttpContext?.User.Identity.Name;
                UserName ??= "Unknown";
            }
            else
            {
                //No HttpContext so seeding data
                UserName = "Seed Data";
            }
        }

        public DbSet<ItemImages> itemImages { get; set; }

        public DbSet<ItemThumbNail> ItemThumbNails { get; set; }


        public DbSet<Item> Items { get; set; }

        public DbSet<Supplier> Suppliers { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Receiving> Orders { get; set; }

        public DbSet<Event> Events { get; set; }

        public DbSet<ItemEvent> ItemEvents { get; set; }

        public DbSet<Location> Locations { get; set; }


        public DbSet<ItemLocation> ItemLocations { get; set; }


        public DbSet<Inventory> Inventories { get; set; }

        public DbSet<Equipment> Equipments { get; set; }

        public DbSet<Employee> Employees { get; set; }


        public DbSet<Subscription> Subscriptions { get; set; }

        public DbSet<InventoryTransfer> InventoryTransfers { get; set; }

        public DbSet<Archive> Archives { get; set; }

        public DbSet<InventoryReportVM> InventoryReports { get; set; }

        public DbSet<ItemReservation> ItemReservations { get; set; }

        public DbSet<EventLog> EventLogs { get; set; }

        public DbSet<MissingItemLog> MissingItemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {


            modelBuilder.Entity<ItemReservation>()
        .HasQueryFilter(p => !p.IsDeleted);



            //Many to Many for Play Table to Musician
            modelBuilder.Entity<ItemLocation>()
                .HasKey(p => new { p.LocationID, p.ItemID });


            //Many to Many for Play Table to Musician
            modelBuilder.Entity<ItemEvent>()
                .HasKey(p => new { p.ItemID, p.EventID });

            modelBuilder.Entity<InventoryTransfer>()
            .HasOne(t => t.Item)
            .WithMany(i => i.InventoryTransfers)
            .HasForeignKey(t => t.ItemId);


            modelBuilder.Entity<InventoryTransfer>()
                .HasOne(t => t.FromLocation)
                .WithMany(l => l.InventoryTransfersFrom)
                .HasForeignKey(t => t.FromLocationId);

            modelBuilder.Entity<InventoryTransfer>()
                .HasOne(t => t.ToLocation)
                .WithMany(l => l.InventoryTransfersTo)
                .HasForeignKey(t => t.ToLocationId);

            modelBuilder.Entity<Item>()
            .HasOne(t => t.Employee)
            .WithMany(l => l.Items)
            .HasForeignKey(t => t.EmployeeID);

            modelBuilder.Entity<ItemReservation>()
        .HasOne(i => i.Item)
        .WithMany(ir => ir.ItemReservations)
        .HasForeignKey(ir => ir.ItemId)
        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MissingItemLog>()
                .HasOne(i => i.Item)
                .WithMany(ir => ir.MissingItemLogs)
                .HasForeignKey(ir => ir.ItemId)
                .OnDelete(DeleteBehavior.Cascade);


            //Add a unique index to the Employee Email
            modelBuilder.Entity<Employee>()
            .HasIndex(a => new {
                a.Email
            })
            .IsUnique();

            //For the InventoryReport ViewModel
            //Note: The Database View name is InventoryReports
            modelBuilder
                .Entity<InventoryReportVM>()
                .ToView(nameof(InventoryReports))
                .HasKey(a => a.ID);


            //Prevents cascade delete from Introment class to musician
            modelBuilder.Entity<ItemLocation>()
                .HasOne<Location>(i => i.Location)
                .WithMany(m => m.ItemLocations)
                .HasForeignKey(m => m.LocationID)
                .OnDelete(DeleteBehavior.Restrict);


            //Prevent Cascade Delete from Location to Inventory
            //so we are prevented from deleting a location with Inventory
            //modelBuilder.Entity<Location>()
            //    .HasMany<Inventory>(i => i.)
            //    .WithOne(l => l.Location)
            //    .HasForeignKey(l => l.LocationID)
            //    .OnDelete(DeleteBehavior.Restrict);
            ////Prevents cascade delete from Introment class to musician
            //modelBuilder.Entity<Musician>()
            //    .HasOne<Instrument>(i => i.Instrument)
            //    .WithMany(m => m.Musicians)
            //    .HasForeignKey(m => m.InstrumentID)
            //    .OnDelete(DeleteBehavior.Restrict);

            ////Cascade Delete for The Play Table
            //modelBuilder.Entity<Play>()
            //    .HasOne(i => i.Instrument)
            //    .WithMany(p => p.Plays)
            //    .HasForeignKey(i => i.InstrumentID)
            //    .OnDelete(DeleteBehavior.Restrict);

            ////Makes a unique key to the SIN of the musician class
            //modelBuilder.Entity<Musician>()
            //    .HasIndex(m => m.SIN)
            //    .IsUnique();
        }

        //The following code is to override both the sync and async SaveChanges methods
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            OnBeforeSaving();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();
            foreach (var entry in entries)
            {
                if (entry.Entity is IAuditable trackable)
                {
                    var now = DateTime.UtcNow;
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            trackable.UpdatedOn = now;
                            trackable.UpdatedBy = UserName;
                            break;

                        case EntityState.Added:
                            trackable.CreatedOn = now;
                            trackable.CreatedBy = UserName;
                            trackable.UpdatedOn = now;
                            trackable.UpdatedBy = UserName;
                            break;
                    }
                }
            }
        }

        public DbSet<CAAMarketing.Models.Category> Category { get; set; }

        public DbSet<CAAMarketing.ViewModels.InventoryReportVM> InventoryReportVM { get; set; }

        //public override int SaveChanges(bool acceptAllChangesOnSuccess)
        //{
        //    //OnBeforeSaving();
        //    return base.SaveChanges(acceptAllChangesOnSuccess);
        //}

        //public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    //OnBeforeSaving();
        //    return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        //}

    }
}
