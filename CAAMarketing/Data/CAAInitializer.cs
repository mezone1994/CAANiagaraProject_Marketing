using CAAMarketing.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAAMarketing.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

public static class CAAInitializer
{
    public static void Seed(IApplicationBuilder applicationBuilder)
    {
        CAAContext context = applicationBuilder.ApplicationServices.CreateScope()
            .ServiceProvider.GetRequiredService<CAAContext>();

        try
        {
            //Delete the database if you need to apply a new Migration
            //context.Database.EnsureDeleted();
            //Create the database if it does not exist and apply the Migration
            context.Database.Migrate(); //This is the same as Update-Database

            //To randomly generate data
            Random rd = new Random();

            // Look for any Doctors.  Since we can't have patients without Doctors.
            if (!context.Suppliers.Any())
            {
                context.Suppliers.AddRange(
                new Supplier
                {
                    Name = "Bic",
                    Email = "bic@gmail.com",
                    Phone = "289-111-1212",
                    Address = "1234 Parker St."

                },
                new Supplier
                {
                    Name = "Rayban",
                    Email = "rayban@gmail.com",
                    Phone = "289-123-1231",
                    Address = "4321 Goearge St."

                },
                new Supplier
                {
                    Name = "Bottley",
                    Email = "bottley@gmail.com",
                    Phone = "289-432-2345",
                    Address = "6543 Hill St."

                },
                new Supplier
                {
                    Name = "Nike",
                    Email = "nike@gmail.com",
                    Phone = "289-656-3432",
                    Address = "8768 Hillery St."

                });
                context.SaveChanges();
            }

            if (!context.Category.Any())
            {
                context.Category.AddRange(
                new Category
                {
                    Name = "Bags",


                },
                new Category
                {
                    Name = "BackPacks",


                },
                new Category
                {
                    Name = "Pens",


                },
                new Category
                {
                    Name = "Bottles",


                },
                new Category
                {
                    Name = "Sunglasses",


                });
                context.SaveChanges();
            }

            // Look for any Doctors.  Since we can't have patients without Doctors.
            if (!context.Items.Any())
            {
                context.Items.AddRange(
                new Item
                {
                    Name = "Studio Pens",
                    Description = "Pens For CAA",
                    Notes = "Pens used for writing",
                    CategoryID = 3,
                    UPC = "" + rd.Next(10000000, 99999999),
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 1,
                },
                new Item
                {
                    Name = "Swag Sunglasses",
                    Description = "Sunglasses for CAA",
                    Notes = "Sunglasses used for writing",
                    CategoryID = 5,
                    UPC = "" + rd.Next(10000000, 99999999),
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 2,
                },
                new Item
                {
                    Name = "Nike Solo Backpack",
                    Description = "Backpacks that comes in many different colors",
                    Notes = "Blue, Pink, Black",
                    CategoryID = 2,
                    UPC = "" + rd.Next(10000000, 99999999),
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 4,
                },
                new Item
                {
                    Name = "Carry-on Bag Nike",
                    Description = "Bags that comes in many different colors",
                    Notes = "Amber, Black, Grey",
                    CategoryID = 1,
                    UPC = "" + rd.Next(10000000, 99999999),
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 4,
                },
                new Item
                {
                    Name = "Stainless-Steel Water Bottles",
                    Description = "Used for storing Water",
                    Notes = "Comes in Black Only",
                    CategoryID = 4,
                    UPC = "" + rd.Next(10000000, 99999999),
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 3,
                });
                context.SaveChanges();
            }

            // Seed locations if there aren't any.
            if (!context.Locations.Any())
            {
                context.Locations.AddRange(
                new Location
                {
                    Name = "Niagara Falls"
                },
                new Location
                {
                    Name = "Thorold"
                },
                new Location
                {
                    Name = "Welland"
                },
                new Location
                {
                    Name = "Fort Erie"
                },
                new Location
                {
                    Name = "St. Catharines"
                }
                );
                context.SaveChanges();
            }

            // Seed events if there aren't any.
            if (!context.Events.Any())
            {
                context.Events.AddRange(
                new Event
                {
                    Name = "Niagara Hotel",
                    Description = "Niagara Hotels 100th year Celebration",
                    Date = DateTime.Parse("2023-05-05"),
                    //location = "Niagara Falls"
                    LocationID = 3

                },
                new Event
                {
                    Name = "CAA Celebrations",
                    Description = "Annual Celebrations for the team",
                    Date = DateTime.Parse("2023-03-05"),
                    //location = "Thorold"
                    LocationID = 2
                },
                new Event
                {
                    Name = "Niagara College Career Fair",
                    Description = "many employers get together",
                    Date = DateTime.Parse("2023-05-05"),
                    //location = "Welland"
                    LocationID = 1

                }
                );
                context.SaveChanges();
            }



            // Seed orders if there aren't any.
            if (!context.Orders.Any())
            {
                context.Orders.AddRange(
                new Order
                {
                    Quantity = 45,
                    DateMade = DateTime.Now,
                    DeliveryDate = DateTime.Parse("2022/03/20"),
                    ItemID = context.Items.FirstOrDefault(d => d.Name == "Carry-on Bag Nike").ID
                },
                new Order
                {
                    Quantity = 70,
                    DateMade = DateTime.Now,
                    DeliveryDate = DateTime.Parse("2022/01/20"),
                    ItemID = context.Items.FirstOrDefault(d => d.Name == "Nike Solo Backpack").ID
                },
                new Order
                {
                    Quantity = 200,
                    DateMade = DateTime.Now,
                    DeliveryDate = DateTime.Parse("2022/02/20"),
                    ItemID = context.Items.FirstOrDefault(d => d.Name == "Stainless-Steel Water Bottles").ID
                },
                new Order
                {
                    Quantity = 45,
                    DateMade = DateTime.Now,
                    DeliveryDate = DateTime.Parse("2022/03/20"),
                    ItemID = context.Items.FirstOrDefault(d => d.Name == "Studio Pens").ID
                });
                context.SaveChanges();
            }
            if (!context.Inventories.Any())
            {

                for (int i = 0; i < context.Set<Item>().Count(); i++)
                {
                    context.Inventories.AddRange(
                        new Inventory
                        {
                            Cost = rd.Next(10, 99),
                            Quantity = rd.Next(10, 999),
                            LocationID = context.Locations.FirstOrDefault(d => d.Name == "Thorold").Id,
                            ItemID = (i + 1)
                        });
                }

                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.GetBaseException().Message);
        }
    }
}