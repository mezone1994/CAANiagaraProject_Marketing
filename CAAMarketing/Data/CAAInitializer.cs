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

            if (!context.Employees.Any())
            {
                context.Employees.AddRange(
                 new Employee
                 {
                     FirstName = "Mahmoud",
                     LastName = "Hachem",
                     Phone = "2891231231",
                     Email = "mhachem12@outlook.com"
                 },
                 new Employee
                 {
                     FirstName = "Fred",
                     LastName = "Flintstone",
                     Phone = "2891231231",
                     Email = "super@caaniagara.ca"
                 },
                 new Employee
                 {
                     FirstName = "Betty",
                     LastName = "Rubble",
                     Phone = "2891231231",
                     Email = "Employee@caaniagara.ca"
                 });

                context.SaveChanges();
            }

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

                },
                new Supplier
                {
                    Name = "Canadian Tire",
                    Email = "canadiantire@gmail.com",
                    Phone = "905-262-4365",
                    Address = "829 Tire Rd."

                },
                new Supplier
                {
                    Name = "The Source",
                    Email = "thesource@gmail.com",
                    Phone = "905-735-0887",
                    Address = "800 Niagara St."

                },
                new Supplier
                {
                    Name = "Dollar Store",
                    Email = "dollarstore@gmail.com",
                    Phone = "905-264-1266",
                    Address = "800 Niagara St."

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


                },
                new Category
                {
                    Name = "Seasonal",


                },
                new Category
                {
                    Name = "Accessories",


                },
                new Category
                {
                    Name = "Apparel",


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
                    EmployeeID = 1
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
                    EmployeeID = 3,

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
                    EmployeeID = 2
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
                    EmployeeID = 1
                },
                new Item
                {
                    Name = "Car Winter Scrapper",
                    Description = "Scrapper for snow/ice remove on vehicles",
                    Notes = "Black, Grey, Blue",
                    CategoryID = 6,
                    UPC = "" + rd.Next(10000000, 99999999),
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 4,
                    EmployeeID = 2
                },
                new Item
                {
                    Name = "Stainless-Steel Water Bottles",
                    Description = "Used for storing Water",
                    Notes = "Comes in Black Only",
                    CategoryID = 4,
                    UPC = "" + rd.Next(10000000, 99999999),
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 5,
                    EmployeeID = 3
                },
                new Item
                {
                    Name = "Laptop Bag",
                    Description = "Used for storing Laptops",
                    Notes = "Comes in Grey Only",
                    CategoryID = 1,
                    UPC = "" + rd.Next(10000000, 99999999),
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 4,
                    EmployeeID = 1
                },
                new Item
                {
                    Name = "Stick on Phone Wallet",
                    Description = "Wallet that sticks to the back of your phone.",
                    Notes = "Comes in Black, Blue and Red",
                    CategoryID = 7,
                    UPC = "" + rd.Next(10000000, 99999999),
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 6,
                    EmployeeID = 2
                },
                new Item
                {
                    Name = "Shirt",
                    Description = "Shirt with CAA Logo, Address, Phone Number and more.",
                    Notes = "Comes in Blue, Grey, Black.",
                    CategoryID = 8,
                    UPC = "" + rd.Next(10000000, 99999999),
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 4,
                    EmployeeID = 3
                },
                new Item
                {
                    Name = "Sticker",
                    Description = "CAA Sticker that can be applied to any surface.",
                    Notes = "Comes in CAA colours.",
                    CategoryID = 7,
                    UPC = "" + rd.Next(10000000, 99999999),
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 7,
                    EmployeeID = 3
                },
                new Item
                {
                    Name = "Key Chain",
                    Description = "Key Chain with CAA logo to attached to someones Car Keys.",
                    Notes = "Comes in CAA colours.",
                    CategoryID = 7,
                    UPC = "" + rd.Next(10000000, 99999999),
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 7,
                    EmployeeID = 1,
                    Archived = true

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
                new Receiving
                {
                    Quantity = 45,
                    DateMade = DateTime.Now,
                    DeliveryDate = DateTime.Parse("2022/03/20"),
                    Cost = 45,
                    ItemID = context.Items.FirstOrDefault(d => d.Name == "Carry-on Bag Nike").ID
                },
                new Receiving
                {
                    Quantity = 70,
                    DateMade = DateTime.Now,
                    DeliveryDate = DateTime.Parse("2022/01/20"),
                    ItemID = context.Items.FirstOrDefault(d => d.Name == "Nike Solo Backpack").ID
                },
                new Receiving
                {
                    Quantity = 200,
                    DateMade = DateTime.Now,
                    DeliveryDate = DateTime.Parse("2022/02/20"),
                    Cost = 55,
                    ItemID = context.Items.FirstOrDefault(d => d.Name == "Stainless-Steel Water Bottles").ID
                },
                new Receiving
                {
                    Quantity = 45,
                    DateMade = DateTime.Now,
                    DeliveryDate = DateTime.Parse("2022/03/20"),
                    Cost = 35,
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
                            LocationID = rd.Next(1, 5),
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