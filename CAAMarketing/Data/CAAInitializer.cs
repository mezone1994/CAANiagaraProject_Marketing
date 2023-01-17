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
            // Look for any Doctors.  Since we can't have patients without Doctors.
            if (!context.Suppliers.Any())
            {
                context.Suppliers.AddRange(
                new Supplier
                {
                    Name = "Penzoil",
                    Email = "penziol@gmail.com", 
                    Phone = "289-111-1212",
                    Address = "1234 Parker St."
                    
                },
                new Supplier
                {
                    Name = "Banzene",
                    Email = "banzene@gmail.com",
                    Phone = "289-123-1231",
                    Address = "4321 Goearge St."

                },
                new Supplier
                {
                    Name = "WeedMan",
                    Email = "weedMan@gmail.com",
                    Phone = "289-432-2345",
                    Address = "6543 Hill St."

                },
                new Supplier
                {
                    Name = "Honda",
                    Email = "honda@gmail.com",
                    Phone = "289-656-3432",
                    Address = "8768 Hillery St."

                });
                context.SaveChanges();
            }
            // Look for any Doctors.  Since we can't have patients without Doctors.
            if (!context.Items.Any())
            {
                context.Items.AddRange(
                new Item
                {
                    Name = "2 Stroke Oil",
                    Description = "Oil For 2 Stroke Engines",
                    Notes = "Oil",
                    Category = "Oil",
                    UPC = "123123e123",
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 1,
                },
                new Item
                {
                    Name = "4 Stroke Oil",
                    Description = "Oil For 4 Stroke Engines",
                    Notes = "Oil",
                    Category = "Oil",
                    UPC = "12432123",
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 2,
                },
                new Item
                {
                    Name = "Washer Fluid",
                    Description = "Fluid For Car Dashes",
                    Notes = "Washer Fliud",
                    Category = "Washer Fluid",
                    UPC = "473662",
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 1,
                },
                new Item
                {
                    Name = "LawnMower",
                    Description = "Used to cut grass",
                    Notes = "LawnMower",
                    Category = "LawnMower",
                    UPC = "43222er221",
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 4,
                },
                new Item
                {
                    Name = "6 Stroke Oil",
                    Description = "Oil For 6 Stroke Engines",
                    Notes = "Oil",
                    Category = "Oil",
                    UPC = "584732123",
                    DateReceived = DateTime.Parse("2022/01/20"),
                    SupplierID = 3,
                });
                context.SaveChanges();
            }

            // Look for any Doctors.  Since we can't have patients without Doctors.
            if (!context.Equipments.Any())
            {
                context.Equipments.AddRange(
                new Equipment
                {
                    Name = "Tent",
                    Description = "Big long Tent"

                },
                new Equipment
                {
                    Name = "Generator",
                    Description = "Gas Deisel Generator 5000KV"

                },
                new Equipment
                {
                    Name = "Table",
                    Description = "6 Chairs Table Set"

                });
                context.SaveChanges();
            }
            // Seed orders if there aren't any.
            if (!context.Users.Any())
            {
                context.Users.AddRange(
                new User
                {
                    UserName = "Ali",
                    loginTime = 2200,
                    ItemID = 1
                },
                new User
                {
                    UserName = "Mahmoud",
                    loginTime = 2200,
                    ItemID = 2
                },
                new User
                {
                    UserName = "Moayed",
                    loginTime = 2200,
                    ItemID = 3
                },
                new User
                {
                    UserName = "Qasem",
                    loginTime = 2200,
                    ItemID = 4
                });
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
                    location = "Thorold",
                    userID = 1
                },
                new Event
                {
                    Name = "CAA Celebrations",
                    Description = "Annual Celebrations for the team",
                    Date = DateTime.Parse("2023-03-05"),
                    location = "Thorold",
                    userID = 2
                },
                new Event
                {
                    Name = "Niagara College Career Fair",
                    Description = "many employers get together",
                    Date = DateTime.Parse("2023-05-05"),
                    location = "Welland",
                    userID = 3
                }
                );
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
            
            // Seed orders if there aren't any.
            if (!context.Orders.Any())
            {
                context.Orders.AddRange(
                new Order
                {
                    Quantity = 45,
                    DateMade = DateTime.Now,
                    DeliveryDate = DateTime.Parse("2022/03/20"),
                    UserID = 1,
                    ItemID = 1
                },
                new Order
                {
                    Quantity = 70,
                    DateMade = DateTime.Now,
                    DeliveryDate = DateTime.Parse("2022/01/20"),
                    UserID = 2,
                    ItemID = 2
                },
                new Order
                {
                    Quantity = 200,
                    DateMade = DateTime.Now,
                    DeliveryDate = DateTime.Parse("2022/02/20"),
                    UserID = 3,
                    ItemID = 3
                },
                new Order
                {
                    Quantity = 45,
                    DateMade = DateTime.Now,
                    DeliveryDate = DateTime.Parse("2022/03/20"),
                    UserID = 1,
                    ItemID = 4
                });
                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.GetBaseException().Message);
        }
    }
}