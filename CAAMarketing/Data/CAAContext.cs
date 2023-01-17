using CAAMarketing.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CAAMarketing.Data
{
    public class CAAContext : DbContext
    {
        public CAAContext(DbContextOptions<CAAContext> options)
            : base(options)
        {
        }


        

        public DbSet<User> Users { get; set; }

        public DbSet<ItemImages> itemImages { get; set; }

        public DbSet<Item> Items { get; set; }

        public DbSet<Supplier> Suppliers { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<Event> Events { get; set; }

        public DbSet<ItemEvent> ItemEvents { get; set; }

        public DbSet<Location> Locations { get; set; }

        public DbSet<Equipment> Equipments { get; set; }

        public DbSet<Inventory> Inventories { get; set; }

        public DbSet<Transfer> Transfers { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.HasDefaultSchema("CAA");


            //Many to Many for Play Table to Musician
            modelBuilder.Entity<ItemEvent>()
                .HasIndex(p => new { p.ItemID, p.EventID })
                .IsUnique();

            modelBuilder.Entity<Transfer>()
                .HasIndex(p => new { p.userID, p.LocationID, p.InventoryID })
                .IsUnique();



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
