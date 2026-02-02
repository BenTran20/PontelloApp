using Microsoft.EntityFrameworkCore;
using PontelloApp.Models;
using PontelloApp.Ultilities;
using System.Diagnostics;

namespace PontelloApp.Data
{
    public class PontelloAppInitializer
    {
        /// <summary>
        /// Prepares the Database and seeds data as required
        /// </summary>
        /// <param name="serviceProvider">DI Container</param>
        /// <param name="DeleteDatabase">Delete the database and start from scratch</param>
        /// <param name="UseMigrations">Use Migrations or EnsureCreated</param>
        /// <param name="SeedSampleData">Add optional sample data</param>
        public static void Initialize(IServiceProvider serviceProvider,
            bool DeleteDatabase = false, bool UseMigrations = true, bool SeedSampleData = true)
        {
            using (var context = new PontelloAppContext(
               serviceProvider.GetRequiredService<DbContextOptions<PontelloAppContext>>()))
            {
                //Refresh the database as per the parameter options
                #region Prepare the Database
                try
                {
                    //Note: .CanConnect() will return false if the database is not there!
                    if (DeleteDatabase || !context.Database.CanConnect())
                    {
                        if (!SqLiteDBUtility.ReallyEnsureDeleted(context)) //Delete the existing version 
                        {
                            Debug.WriteLine("Could not clear the old version " +
                                "of the database out of the way.  You will need to exit " +
                                "Visual Studio and try to do it manually.");
                        }

                        if (UseMigrations)
                        {
                            context.Database.Migrate(); //Create the Database and apply all migrations
                        }
                        else
                        {
                            context.Database.EnsureCreated(); //Create and update the database as per the Model
                        }
                        //Here is a good place to create any additional database objects such as Triggers or Views
                        //----------------------------------------------------------------------------------------
                    }
                    else //The database is already created
                    {
                        if (UseMigrations)
                        {
                            context.Database.Migrate(); //Apply all migrations
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.GetBaseException().Message);
                }
                #endregion

                //Seed data needed
                #region Seed Required Data - Pontello
                try
                {
                    // -------- Seed Categories --------
                    if (!context.Categories.Any())
                    {
                        context.Categories.AddRange(
                            new Category { Name = "Steering" },
                            new Category { Name = "Chassis" },
                            new Category { Name = "Hardware" },
                            new Category { Name = "Wheels" },
                            new Category { Name = "Seats" },
                            new Category { Name = "Accessories" },
                            new Category { Name = "Uncategorized" }
                        );
                        context.SaveChanges();
                    }

                    // -------- Seed Products --------
                    if (!context.Products.Any())
                    {
                        context.Products.AddRange(
                            new Product //1
                            {
                                ProductName = "1/2\" Camber Rod End",
                                Description = "The 1/2\" Camber Rod End is a durable, precision-machined chassis component for Charger Racing Chassis, engineered for high-performance kart applications.",
                                IsActive = true,
                                CategoryID = context.Categories.First(c => c.Name == "Steering").ID
                            },
                            new Product //2
                            {
                                ProductName = "1/2 - 20 Hex Slotted Jam Nut",
                                Description = "The 1/2 - 20 Hex Slotted Jam Nut is a durable and precision-machined fastener designed for kart chassis applications.",
                                IsActive = false,
                                CategoryID = context.Categories.First(c => c.Name == "Hardware").ID,
                            },
                            new Product //3
                            {
                                ProductName = "6 1/2\" Tie Rod",
                                Description = "The Tie Rod (6-1/2\") by Charger Racing Chassis is a durable steering linkage component designed to maintain accurate toe settings and consistent steering response.",
                                CategoryID = context.Categories.First(c => c.Name == "Steering").ID,
                                IsActive = true
                            },
                            new Product //4
                            {
                                ProductName = "Pedal Grips",
                                Description = "Pedal Grips with logo engraved. Two to a set.",
                                CategoryID = context.Categories.First(c => c.Name == "Accessories").ID,
                                IsActive = true
                            },
                            new Product //5
                            {
                                ProductName = "Champ Buggy Chassis Package",
                                Description = "The Champ Buggy Chassis Package Silver Edition from Charger Racing Chassis is a complete, race-ready chassis system built for high performance, durability, and ease of maintenance.",
                                CategoryID = context.Categories.First(c => c.Name == "Chassis").ID,
                                IsActive = true
                            },
                            new Product //6
                            {
                                ProductName = "Tie Rod",
                                Description = "The Tie Rod (11-3/4\") by Charger Racing Chassis is a durable steering linkage component designed to maintain accurate toe settings and consistent steering response.",
                                CategoryID = context.Categories.First(c => c.Name == "Steering").ID,
                                IsActive = true
                            },
                            new Product //7
                            {
                                ProductName = "Tie Rod Ends",
                                Description = "The Tie Rod End (3/8\") by Charger Racing Chassis is a precision steering linkage component designed to provide smooth articulation and reliable steering response.",
                                CategoryID = context.Categories.First(c => c.Name == "Steering").ID,
                                IsActive = true
                            },
                            new Product //8
                            {
                                ProductName = "Tie Rod Jam Nut",
                                Description = "The Tie Rod Jam Nut by Charger Racing Chassis is a precision-threaded locking fastener designed to secure tie rod ends and maintain consistent toe alignment.",
                                CategoryID = context.Categories.First(c => c.Name == "Hardware").ID,
                                IsActive = true
                            },
                            new Product //9
                            {
                                ProductName = "1/2\" Camber Rod End Assembly",
                                Description = "The 1/2\" Camber Rod End Assembly is a precision-engineered component from Charger Racing Chassis, designed for reliability and smooth articulation within the kart’s steering and suspension geometry",
                                CategoryID = context.Categories.First(c => c.Name == "Steering").ID,
                                IsActive = true
                            },
                            new Product //10
                            {
                                ProductName = "Axle Assembly",
                                Description = "The Axle Assembly Charger Racing Chassis is a complete, ready-to-install rear axle system designed for performance, durability, and precision.",
                                CategoryID = context.Categories.First(c => c.Name == "Chassis").ID,
                                IsActive = true
                            },
                            new Product //11
                            {
                                ProductName = "JKB Fiberglass Seat",
                                Description = "The JKB Fiberglass Seat provides a durable, lightweight, and performance-driven seating solution for kart racers.",
                                CategoryID = context.Categories.First(c => c.Name == "Seats").ID,
                                IsActive = true
                            },
                            new Product //12
                            {
                                ProductName = "Van-K Non-machined 8\" Wheels",
                                Description = "6\", 7\", or 8\" width — 8\" diameter, 3\" back‑space, American 4×4.00\" (2.50\" register)\r\n\r\n9\" " +
                                              "width — 8\" diameter, 4\" back‑space, American 4×4.00\" (2.50\" register)\r\n\r\n10\" " +
                                              "width — 8\" diameter, 5\" back‑space, American 4×4.00\" (2.50\" register)",
                                CategoryID = context.Categories.First(c => c.Name == "Wheels").ID,
                                IsActive = true
                            },
                            new Product //13
                            {
                                ProductName = "Lower Steering Upright Bolt Assembly",
                                Description = "The Lower Steering Upright Bolt Assembly Charger Racing Chassis is a complete, race-ready hardware set designed to secure the Steering shaft to the tie rods.",
                                CategoryID = context.Categories.First(c => c.Name == "Hardware").ID,
                                IsActive = true
                            },
                            new Product //14
                            {
                                ProductName = "Lower Steering Rod End with Jam Nut",
                                Description = "The Lower Steering Rod End with Jam Nut from Charger Racing Chassis is a precision-engineered heim joint designed for the lower steering linkage.",
                                CategoryID = context.Categories.First(c => c.Name == "Uncategorized").ID,
                                IsActive = true
                            },
                            new Product //15
                            {
                                ProductName = "Steering Wheel Hex Slotted Nut 1/2 - 20",
                                Description = "The Steering Wheel Hex Slotted Nut (1/2\"-20) by Charger Racing Chassis is a zinc-finished fastening component designed to securely retain the steering wheel and hub assembly on the steering shaft.",
                                CategoryID = context.Categories.First(c => c.Name == "Hardware").ID,
                                IsActive = true
                            },
                            new Product //16
                            {
                                ProductName = "Splined Steering Wheel Hub & Bolt Assembly",
                                Description = "The Splined Steering Wheel Hub Bolt Assembly by Charger Racing Chassis is a precision-machined steering component designed to provide a secure, slop-free connection between the steering shaft and wheel.",
                                CategoryID = context.Categories.First(c => c.Name == "Steering").ID,
                                IsActive = true
                            },
                            new Product //17
                            {
                                ProductName = "Quick Release 5/8\" Steering Wheel Hub for Champ",
                                Description = "The Quick Release 5/8\" Steering Wheel Hub for Champ from Charger Racing Chassis is a precision-engineered steering component designed to deliver fast, secure, and reliable wheel removal during kart setup, transport, and maintenance.",
                                CategoryID = context.Categories.First(c => c.Name == "Uncategorized").ID,
                                IsActive = true
                            },
                            new Product //18
                            {
                                ProductName = "Steering Toe Lock Kit",
                                Description = "The Steering Toe Lock Kit by Charger Racing Chassis is a precision steering component designed to securely lock toe settings and prevent unwanted adjustment during racing.",
                                CategoryID = context.Categories.First(c => c.Name == "Uncategorized").ID,
                                IsActive = true
                            },
                            new Product //19
                            {
                                ProductName = "Tach Mount",
                                Description = "The Tach Mount by Charger Racing Chassis is a bent mounting bracket designed to securely position a tachometer within the driver’s line of sight.",
                                CategoryID = context.Categories.First(c => c.Name == "Uncategorized").ID,
                                IsActive = true
                            },
                            new Product //20
                            {
                                ProductName = "Caster Block Safety Pin Assembly",
                                Description = "The Caster Block Safety Pin Assembly from Charger Racing Chassis is a precision-engineered safety component designed to secure the caster block assembly and prevent unwanted movement or separation " +
                                              "during racing. Built for reliability, this assembly adds an extra layer of security to the front-end setup, ensuring consistent handling and durability under high-stress conditions.",
                                CategoryID = context.Categories.First(c => c.Name == "Accessories").ID,
                                IsActive = true
                            }
                            );
                        context.SaveChanges();
                    }

                    // -------- Seed Variant --------
                    if (!context.ProductVariants.Any())
                    {
                        context.ProductVariants.AddRange(
                            new ProductVariant //1
                            {
                                ProductId = 1,
                                UnitPrice = 12.99m,
                                StockQuantity = 30,
                                SKU_ExternalID = "1140",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Type", Value="Steel" }
                                    }
                            },
                            new ProductVariant //1
                            {
                                ProductId = 1,
                                UnitPrice = 12.99m,
                                StockQuantity = 20,
                                SKU_ExternalID = "1140",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Type", Value="Metal" }
                                    }
                            },
                            new ProductVariant //2
                            {
                                ProductId = 2,
                                UnitPrice = 2.47m,
                                StockQuantity = 33,
                                SKU_ExternalID = "1142",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Title", Value="Default" }
                                    }
                            },
                             new ProductVariant //3
                             {
                                 ProductId = 3,
                                 UnitPrice = 20.45m,
                                 StockQuantity = 90,
                                 SKU_ExternalID = "1075",
                                 Options = new List<Variant>
                                    {
                                       new Variant { Name="Type", Value="6 1/2\" Tie Rod Left" }
                                    }
                             },
                            new ProductVariant //4
                            {
                                ProductId = 4,
                                UnitPrice = 28.25m,
                                StockQuantity = 70,
                                SKU_ExternalID = "1188",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Brand", Value="Charger Pedal Grips"}
                                    }
                            },
                            new ProductVariant //5 (Option 1)
                            {
                                ProductId = 5,
                                UnitPrice = 4850m,
                                StockQuantity = 2,
                                SKU_ExternalID = "1096",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Chassis", Value="Sr.Champ" },
                                    }
                            },
                            new ProductVariant //5 (Option 2)
                            {
                                ProductId = 5,
                                UnitPrice = 4850m,
                                StockQuantity = 10,
                                SKU_ExternalID = "1096",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Chassis", Value="Jr. Sportsman Champ" },
                                    }
                            },
                            new ProductVariant //6
                            {
                                ProductId = 6,
                                UnitPrice = 14.3m,
                                StockQuantity = 30,
                                SKU_ExternalID = "1033",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Type", Value="6 1/2\" Tie Rod Left" }
                                    }
                            },
                            new ProductVariant //6
                            {
                                ProductId = 6,
                                UnitPrice = 14.3m,
                                StockQuantity = 22,
                                SKU_ExternalID = "1033",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Type", Value="6 1/2\" Tie Rod Right" }
                                    }
                            },
                            new ProductVariant //7
                            {
                                ProductId = 7,
                                UnitPrice = 9.75m,
                                StockQuantity = 26,
                                SKU_ExternalID = "1078",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Type", Value="3/8\" Right Rod End" }
                                    }
                            },
                            new ProductVariant //8
                            {
                                ProductId = 8,
                                UnitPrice = 1.3m,
                                StockQuantity = 11,
                                SKU_ExternalID = "1080",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Hand", Value="Left" }
                                    }
                            },
                            new ProductVariant //8
                            {
                                ProductId = 8,
                                UnitPrice = 1.3m,
                                StockQuantity = 20,
                                SKU_ExternalID = "1080",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Hand", Value="Right" }
                                    }
                            },
                            new ProductVariant //9
                            {
                                ProductId = 9,
                                UnitPrice = 22.09m,
                                StockQuantity = 40,
                                SKU_ExternalID = "1141",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Title", Value="Default" }
                                    }
                            },
                            new ProductVariant //10
                            {
                                ProductId = 10,
                                UnitPrice = 508.5m,
                                StockQuantity = 4,
                                SKU_ExternalID = "1150",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Bearing Size", Value="206(small)" }
                                    }
                            },
                            new ProductVariant //11 (Option 1)
                            {
                                ProductId = 11,
                                UnitPrice = 129.95m,
                                StockQuantity = 22,
                                SKU_ExternalID = "se-jrsm",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Style", Value="Evolution" },
                                       new Variant { Name="Size",  Value="Small" }
                                    }
                            },
                            new ProductVariant //11 (Option 2)
                            {
                                ProductId = 11,
                                UnitPrice = 129.95m,
                                StockQuantity = 10,
                                SKU_ExternalID = "se-jrsm",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Style", Value="Evolution" },
                                       new Variant { Name="Size",  Value="Medium" }
                                    }
                            },
                            new ProductVariant //12
                            {
                                ProductId = 12,
                                UnitPrice = 74.38m,
                                StockQuantity = 42,
                                SKU_ExternalID = "VK-180.8600.3.44",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Size", Value="6\" Wide" },
                                       new Variant { Name="Finish", Value="Polished" }
                                    }
                            },
                            new ProductVariant //13
                            {
                                ProductId = 13,
                                UnitPrice = 5.65m,
                                StockQuantity = 15,
                                SKU_ExternalID = "1086",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Title", Value="Default" }
                                    }
                            },
                            new ProductVariant //14
                            {
                                ProductId = 14,
                                UnitPrice = 15.6m,
                                StockQuantity = 31,
                                SKU_ExternalID = "1088",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Title", Value="Default" }
                                    }
                            },
                            new ProductVariant //15
                            {
                                ProductId = 15,
                                UnitPrice = 1.62m,
                                StockQuantity = 9,
                                SKU_ExternalID = "1089",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Title", Value="Default" }
                                    }
                            },
                            new ProductVariant //16
                            {
                                ProductId = 16,
                                UnitPrice = 16.95m,
                                StockQuantity = 8,
                                SKU_ExternalID = "1090",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Title", Value="Default" }
                                    }
                            },
                            new ProductVariant //17
                            {
                                ProductId = 17,
                                UnitPrice = 57.18m,
                                StockQuantity = 41,
                                SKU_ExternalID = "1091",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Title", Value="Default" }
                                    }
                            },
                            new ProductVariant //18
                            {
                                ProductId = 18,
                                UnitPrice = 37.29m,
                                StockQuantity = 5,
                                SKU_ExternalID = "1092",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Title", Value="Default" }
                                    }
                            },
                            new ProductVariant //19
                            {
                                ProductId = 19,
                                UnitPrice = 25.99m,
                                StockQuantity = 13,
                                SKU_ExternalID = "1093",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Title", Value="Default" }
                                    }
                            },
                            new ProductVariant //20
                            {
                                ProductId = 20,
                                UnitPrice = 15.59m,
                                StockQuantity = 30,
                                SKU_ExternalID = "1137",
                                Options = new List<Variant>
                                    {
                                       new Variant { Name="Title", Value="Default" }
                                    }
                            }
                            );
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.GetBaseException().Message);
                }
                #endregion
            }
        }
    }
}
