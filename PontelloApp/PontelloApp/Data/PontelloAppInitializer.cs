using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Models;
using PontelloApp.Ultilities;
using Product = PontelloApp.Models.Product;

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
                        string sqlCmd = @"
                            CREATE TRIGGER SetProductTimestampOnUpdate
                            AFTER UPDATE ON Products
                            BEGIN
                                UPDATE Products
                                SET RowVersion = randomblob(8)
                                WHERE rowid = NEW.rowid;
                            END;
                        ";
                        context.Database.ExecuteSqlRaw(sqlCmd);

                        sqlCmd = @"
                            CREATE TRIGGER SetProductTimestampOnInsert
                            AFTER INSERT ON Products
                            BEGIN
                                UPDATE Products
                                SET RowVersion = randomblob(8)
                                WHERE rowid = NEW.rowid;
                            END
                        ";
                        context.Database.ExecuteSqlRaw(sqlCmd);

                        sqlCmd = @"
                            CREATE TRIGGER SetProductVariantTimestampOnUpdate
                            AFTER UPDATE ON ProductVariants
                            BEGIN
                                UPDATE ProductVariants
                                SET RowVersion = randomblob(8)
                                WHERE rowid = NEW.rowid;
                            END;
                        ";
                        context.Database.ExecuteSqlRaw(sqlCmd);

                        sqlCmd = @"
                            CREATE TRIGGER SetProductVariantTimestampOnInsert
                            AFTER INSERT ON ProductVariants
                            BEGIN
                                UPDATE ProductVariants
                                SET RowVersion = randomblob(8)
                                WHERE rowid = NEW.rowid;
                            END
                        ";
                        context.Database.ExecuteSqlRaw(sqlCmd);
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
                            new Category { Name = "Uncategorized" },
                            new Category { Name = "Steering" },
                            new Category { Name = "Chassis" },
                            new Category { Name = "Hardware" },
                            new Category { Name = "Wheels" },
                            new Category { Name = "Accessories" },
                            new Category { Name = "Vehicles & Parts" }
                        );
                        context.SaveChanges();

                        var catHardwareId = context.Categories.First(c => c.Name == "Hardware").ID;
                        var catVehiclesPartsId = context.Categories.First(c => c.Name == "Vehicles & Parts").ID;

                        context.Categories.AddRange(
                            new Category { Name = "Fasteners", ParentCategoryID = catHardwareId },
                            new Category { Name = "Components", ParentCategoryID = catHardwareId },
                            new Category { Name = "Vehicle Parts & Accessories", ParentCategoryID = catVehiclesPartsId }
                        );
                        context.SaveChanges();

                        var catVehiclePartsAccessoriesId =
                            context.Categories.First(c => c.Name == "Vehicle Parts & Accessories").ID;

                        context.Categories.Add(
                            new Category
                            {
                                Name = "Motor Vehicle Parts",
                                ParentCategoryID = catVehiclePartsAccessoriesId
                            });
                        context.SaveChanges();

                        var catMotorVehiclePartsId =
                            context.Categories.First(c => c.Name == "Motor Vehicle Parts").ID;

                        context.Categories.AddRange(
                            new Category { Name = "Bumpers", ParentCategoryID = catMotorVehiclePartsId },
                            new Category { Name = "Steering Racks", ParentCategoryID = catMotorVehiclePartsId }
                        );
                        context.SaveChanges();
                    }

                    // -------- Seed Vendors --------
                    if (!context.Vendors.Any())
                    {
                        context.Vendors.AddRange(
                            new Vendor { Name = "Charger Racing Chassis" },
                            new Vendor { Name = "Authentic Phantom Component" },
                            new Vendor { Name = "AiM Technology" },
                            new Vendor { Name = "Pontello Motorsports" },
                            new Vendor { Name = "Performance" },
                            new Vendor { Name = "Speed Karts" },
                            new Vendor { Name = "MCP" },
                            new Vendor { Name = "PMI" },
                            new Vendor { Name = "Ultramax" },
                            new Vendor { Name = "RacingTech" },
                            new Vendor { Name = "Universal Parts" },
                            new Vendor { Name = "K1 RaceGear" },
                            new Vendor { Name = "RLV" },
                            new Vendor { Name = "Burris Racing" },
                            new Vendor { Name = "AMV Wheels" },
                            new Vendor { Name = "Vega Tires" },
                            new Vendor { Name = "Hoosier Racing Tire" }
                        );

                        context.SaveChanges();
                    }

                    // -------- Seed Products --------
                    if (!context.Products.Any())
                    {
                        // Vendor IDs
                        var racingTechId = context.Vendors.First(v => v.Name == "RacingTech").VendorID;
                        var chargerId = context.Vendors.First(v => v.Name == "Charger Racing Chassis").VendorID;
                        var aimId = context.Vendors.First(v => v.Name == "AiM Technology").VendorID;
                        var authenticId = context.Vendors.First(v => v.Name == "Authentic Phantom Component").VendorID;

                        // Category IDs
                        var catWheelsId = context.Categories.First(c => c.Name == "Wheels").ID;
                        var catComponentsId = context.Categories.First(c => c.Name == "Components").ID;
                        var catUncategorizedId = context.Categories.First(c => c.Name == "Uncategorized").ID;
                        var catBumpersId = context.Categories.First(c => c.Name == "Bumpers").ID;
                        var catMotorPartsId = context.Categories.First(c => c.Name == "Motor Vehicle Parts").ID;

                        context.Products.AddRange(
                            new Product //1
                            {
                                ProductName = "Ultra Racing Wheel",
                                Description = "High-performance racing wheel suitable for kart and small vehicles.",
                                IsActive = false,
                                CategoryID = catWheelsId,
                                Handle = "ultra-racing-wheel",
                                VendorID = racingTechId,
                                Tag = "Racing Wheel"
                            },
                            new Product //2
                            {
                                ProductName = "Pedals",
                                Description = "<p data-start=\"183\" data-end=\"572\">The<strong data-start=\"206\" data-end=\"224\"> Pedals</strong> from <strong data-start=\"230\" data-end=\"256\">Charger Racing Chassis</strong> is a durable, race-ready replacement designed for precise control and consistent feel on the track. Featuring a strong, lightweight construction and a smooth actuation profile, this pedal is built to withstand the demands of competitive karting while maintaining reliable performance season after season.</p>\r\n<p data-start=\"574\" data-end=\"761\">Perfect for new builds, maintenance, or replacing worn components, these pedals install easily on compatible Charger chassis models and provides the responsiveness drivers expect.</p>\r\n<p data-start=\"763\" data-end=\"973\"><strong data-start=\"763\" data-end=\"779\">Application:</strong><br data-start=\"779\" data-end=\"782\">Ideal for use on Charger Racing Chassis karts requiring a replacement or upgrade to accommodate drivers needs. Install as specified for throttle control systems to restore smooth, predictable driver input.</p>",
                                IsActive = true,
                                CategoryID = catComponentsId,
                                Handle = "pedals",
                                VendorID = chargerId,
                                Tag = "Throttle & Brake Controls",
                                Type = "Throttle & Brake Controls"
                            },
                            new Product //3
                            {
                                ProductName = "Tie Rod",
                                Description = "<p data-start=\"258\" data-end=\"711\">The Tie Rod (11-3/4\") by <strong data-start=\"301\" data-end=\"327\">Charger Racing Chassis</strong> is a durable steering linkage component designed to maintain accurate toe settings and consistent steering response. Built for reliable fitment and long-term durability, this tie rod helps transmit steering input smoothly while withstanding vibration and load encountered in competitive karting environments. Precision threading ensures repeatable adjustment and secure installation.</p>\r\n<p data-start=\"713\" data-end=\"1068\"><strong data-start=\"713\" data-end=\"729\">Application:</strong><br data-start=\"729\" data-end=\"732\">Used as part of the steering system on Charger and compatible Platinum chassis platforms to connect the steering yoke to the spindle arm. Ideal for steering system maintenance, alignment adjustment, or replacing worn tie rods. Suitable for Prodigy and Prodigy Cadet configurations—verify length and side orientation before installation.</p>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "tie-rod",
                                VendorID = chargerId,
                                Tag = "Platinum, Prodigy, prodigy cadet, Steering Components, Tie Rods",
                                Type = "Steering Components"
                            },
                            new Product //4
                            {
                                ProductName = "Steering Toe Lock Kit",
                                Description = "<p data-start=\"258\" data-end=\"717\">The Steering Toe Lock Kit by <strong data-start=\"306\" data-end=\"332\">Charger Racing Chassis</strong> is a precision steering component designed to securely lock toe settings and prevent unwanted adjustment during racing. Built for durability and repeatable fitment, this kit helps maintain consistent front-end alignment while withstanding vibration and steering loads. Its simple, effective design makes it an essential component for stable handling and reliable steering performance.</p>\r\n<p data-start=\"719\" data-end=\"1012\"><strong data-start=\"719\" data-end=\"735\">Application:</strong><br data-start=\"735\" data-end=\"738\">Used on Charger chassis steering systems to lock and retain toe alignment. Ideal for new builds, front-end setup tuning, or replacing worn or missing toe-lock hardware. Suitable for competitive karting applications where precise and repeatable steering geometry is required.</p>\r\n<hr data-start=\"1014\" data-end=\"1017\">\r\n<h3 data-start=\"1019\" data-end=\"1035\"><strong data-start=\"1023\" data-end=\"1035\">Includes</strong></h3>\r\n<ul data-start=\"1036\" data-end=\"1102\">\r\n<li data-start=\"1036\" data-end=\"1058\">\r\n<p data-start=\"1038\" data-end=\"1058\">(1) steering block</p>\r\n</li>\r\n<li data-start=\"1059\" data-end=\"1080\">\r\n<p data-start=\"1061\" data-end=\"1080\">(1) steering lock</p>\r\n</li>\r\n<li data-start=\"1081\" data-end=\"1102\">\r\n<p data-start=\"1083\" data-end=\"1102\">(1) pin and cable</p>\r\n</li>\r\n</ul>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "steering-toe-lock-kit",
                                VendorID = chargerId,
                                Tag = "Steering Components",
                                Type = "Steering Components"
                            },
                            new Product //5
                            {
                                ProductName = "1/2\" ID Kingpin Washer",
                                Description = "<p data-start=\"150\" data-end=\"749\">The 1/2\" ID Kingpin Washer is a precision-machined component from <strong data-start=\"376\" data-end=\"402\">Charger Racing Chassis</strong>, designed to provide proper spacing and alignment in kingpin assemblies. Crafted from durable materials, this washer ensures consistent performance, reduces wear, and maintains smooth spindle movement during high-stress racing conditions. It’s an essential part for chassis builders and racers maintaining top-level performance and reliability.</p>\r\n<p data-start=\"751\" data-end=\"770\"><strong data-start=\"751\" data-end=\"767\">Key Features</strong>:</p>\r\n<ul data-start=\"771\" data-end=\"1239\">\r\n<li data-start=\"771\" data-end=\"888\">\r\n<p data-start=\"773\" data-end=\"888\"><strong data-start=\"773\" data-end=\"790\">Precision Fit</strong>: Engineered for exact alignment within the kingpin assembly to maintain proper spindle spacing.</p>\r\n</li>\r\n<li data-start=\"889\" data-end=\"1003\">\r\n<p data-start=\"891\" data-end=\"1003\"><strong data-start=\"891\" data-end=\"911\">Durable Material</strong>: Built to withstand heavy loads, impact, and vibration in competitive karting conditions.</p>\r\n</li>\r\n<li data-start=\"1004\" data-end=\"1126\">\r\n<p data-start=\"1006\" data-end=\"1126\"><strong data-start=\"1006\" data-end=\"1032\">Consistent Performance</strong>: Reduces friction and wear while improving the longevity of spindle and kingpin components.</p>\r\n</li>\r\n<li data-start=\"1127\" data-end=\"1239\">\r\n<p data-start=\"1129\" data-end=\"1239\"><strong data-start=\"1129\" data-end=\"1152\">Optimized Thickness</strong>: .060\" and 0.30\" designs ensures accurate spacing for smooth and responsive steering operation.</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1241\" data-end=\"1556\"><strong data-start=\"1241\" data-end=\"1256\">Application</strong>:<br data-start=\"1257\" data-end=\"1260\">Install as specified in kart chassis assembly or during maintenance. Confirm correct sizing and fitment prior to installation. Compatible with <strong data-start=\"1403\" data-end=\"1413\">Legacy</strong>, <strong data-start=\"1415\" data-end=\"1431\">Legacy Cadet</strong>, <strong data-start=\"1433\" data-end=\"1443\">Magnum</strong>, <strong data-start=\"1445\" data-end=\"1456\">Prodigy</strong>, and <strong data-start=\"1462\" data-end=\"1479\">Prodigy Cadet</strong> chassis, as well as other models using similar spindle and kingpin setups.</p>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "1-2-id-kingpin-washer",
                                VendorID = chargerId,
                                Tag = "Kingpin Items, Legacy, legacy cadet, Magnum, Prodigy, prodigy cadet, spindle, Spindle Items",
                                Type = "Spindle Items"
                            },
                            new Product //6
                            {
                                ProductName = "Fuel Tanks",
                                Description = "<p data-start=\"356\" data-end=\"857\">The<strong data-start=\"379\" data-end=\"411\"> Fuel Tanks</strong> from <strong data-start=\"417\" data-end=\"443\">Charger Racing Chassis</strong> is a high-quality, race-ready fuel cell designed for reliability, safety, and performance. Built from durable materials to withstand the rigors of competitive karting, this fuel tank offers consistent fuel delivery and a compact design ideal for upright chassis mounting. Perfect for racers seeking efficient fuel storage and flow in tight spaces, it’s engineered for both ease of maintenance and long-term use.</p>\r\n<p data-start=\"859\" data-end=\"878\"><strong data-start=\"859\" data-end=\"875\">Key Features</strong>:</p>...",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "fuel-tank-3qt-up-right",
                                VendorID = chargerId,
                                Tag = "fuel, fuel can, fuel cell, Fuel Tanks, gas cell, gas tank",
                                Type = "Fuel Tanks"
                            },
                            new Product //7
                            {
                                ProductName = "Seat Saver Kit",
                                Description = "<p data-start=\"232\" data-end=\"699\">The Seat Saver Kit by <strong data-start=\"273\" data-end=\"299\">Charger Racing Chassis</strong> is designed to reinforce fiberglass seat mounting points and extend seat life under regular racing use. These rivet-in washers replace standard 5/16\" flat washers, creating a stronger, more secure mounting surface that prevents bolt pull-through and minimizes wear around drilled holes. Built for durability and simplicity, this kit offers an easy, effective upgrade to protect your seat investment.</p>\r\n<p data-start=\"701\" data-end=\"994\"><strong data-start=\"701\" data-end=\"717\">Application:</strong><br data-start=\"717\" data-end=\"720\">Used on fiberglass racing seats across Charger chassis models to reinforce mounting locations and maintain proper seat fitment. Ideal for new seat installations or refurbishing worn mounting holes. Rivets directly into the seat for a long-lasting, service-friendly solution.</p>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "seat-saver-kit",
                                VendorID = chargerId,
                                Tag = "Bodywork, Dynasty, fiberglass, Legacy, legacy cadet, Magnum, Prodigy, prodigy cadet, Tyrant",
                                Type = "Seats & Accessories"
                            },
                            new Product //8
                            {
                                ProductName = "Rear Bumper Mounting Kit",
                                Description = "<p data-start=\"325\" data-end=\"691\"><strong>The Rear Bumper Mounting Kit</strong> from <strong>Charger Racing Chassis </strong>is a durable, track-ready hardware kit designed to securely mount rear bumpers across multiple Charger chassis models. Built to withstand the stress of competition, this kit uses high-quality fasteners to ensure a tight, reliable bumper installation that maintains structural support and safety during racing.</p>\r\n<h3 data-start=\"693\" data-end=\"715\"><strong data-start=\"697\" data-end=\"713\">Application:</strong></h3>\r\n<p data-start=\"716\" data-end=\"948\">Install as specified for your Charger chassis during bumper setup or replacement. Ideal for routine maintenance, new builds, or replacing worn or damaged hardware. Always confirm sizing and chassis compatibility before installation.</p>\r\n<h3 data-start=\"950\" data-end=\"976\"><strong data-start=\"954\" data-end=\"974\">Compatible With:</strong></h3>\r\n<ul data-start=\"977\" data-end=\"1081\">\r\n<li data-start=\"977\" data-end=\"988\">\r\n<p data-start=\"979\" data-end=\"988\">Charger</p>\r\n</li>\r\n<li data-start=\"989\" data-end=\"1014\">\r\n<p data-start=\"991\" data-end=\"1014\">Legacy &amp; Legacy Cadet</p>\r\n</li>\r\n<li data-start=\"1015\" data-end=\"1025\">\r\n<p data-start=\"1017\" data-end=\"1025\">Magnum</p>\r\n</li>\r\n<li data-start=\"1026\" data-end=\"1053\">\r\n<p data-start=\"1028\" data-end=\"1053\">Prodigy &amp; Prodigy Cadet</p>\r\n</li>\r\n<li data-start=\"1054\" data-end=\"1081\">\r\n<p data-start=\"1056\" data-end=\"1081\">Loop-Style Rear Bumpers</p>\r\n</li>\r\n</ul>",
                                IsActive = true,
                                CategoryID = catBumpersId,
                                Handle = "2020-rear-bumper-mounting-kit",
                                VendorID = chargerId,
                                Tag = "charger, Legacy, legacy cadet, loop bumper, Magnum, Nerf Bars and Bumpers, Prodiggy, Prodigy, prodigy cadet",
                                Type = "Nerf Bars and Bumpers"
                            },
                            new Product //9
                            {
                                ProductName = "Rotary Throttle Position Sensor",
                                Description = "<p>Rotary Design: The rotary-style throttle position sensor tracks the rotational movement of the throttle valve for precise throttle position measurements.<br>\r\nReal-Time Data: Provides instant feedback on throttle inputs, helping teams analyze driver behavior and engine response.<br>\r\nDurable: Built to withstand high temperatures, vibrations, and other harsh motorsport conditions.<br>\r\nIdeal For: Motorsport Teams needing to monitor throttle inputs for performance tuning and driver analysis. Karting and Racing Teams or engineers needing to integrate throttle position data for vehicle tuning and driver performance improvement.</p>",
                                IsActive = true,
                                CategoryID = catMotorPartsId,
                                Handle = "rotary-throttle-position-sensor",
                                VendorID = aimId,
                                Tag = "Sensors",
                                Type = "Electronic Hardware"
                            },
                            new Product //10
                            {
                                ProductName = "Floor Pan Bolt Kit",
                                Description = "<p data-start=\"331\" data-end=\"741\">The <strong data-start=\"354\" data-end=\"376\">Floor Pan Bolt Kit</strong> from <strong data-start=\"382\" data-end=\"408\">Charger Racing Chassis</strong> provides all the essential hardware required for securely mounting the floor pan to your kart chassis. Built from durable, corrosion-resistant materials, this kit ensures long-lasting reliability and easy installation. Each fastener is precision-made to deliver consistent fitment and performance in demanding racing environments.</p>\r\n<p data-start=\"743\" data-end=\"762\"><strong data-start=\"743\" data-end=\"759\">Key Features</strong>:</p>\r\n<ul data-start=\"763\" data-end=\"1190\">\r\n<li data-start=\"763\" data-end=\"869\">\r\n<p data-start=\"765\" data-end=\"869\"><strong data-start=\"765\" data-end=\"790\">Complete Mounting Kit</strong>: Includes all bolts, nuts, and washers necessary for floor pan installation.</p>\r\n</li>\r\n<li data-start=\"870\" data-end=\"978\">\r\n<p data-start=\"872\" data-end=\"978\"><strong data-start=\"872\" data-end=\"896\">Durable Construction</strong>: High-quality steel hardware designed to resist vibration, wear, and corrosion.</p>\r\n</li>\r\n<li data-start=\"979\" data-end=\"1087\">\r\n<p data-start=\"981\" data-end=\"1087\"><strong data-start=\"981\" data-end=\"1002\">Easy Installation</strong>: Pre-sized components ensure a perfect fit with Charger Racing Chassis floor pans.</p>\r\n</li>\r\n<li data-start=\"1088\" data-end=\"1190\">\r\n<p data-start=\"1090\" data-end=\"1190\"><strong data-start=\"1090\" data-end=\"1113\">Professional Finish</strong>: Provides a secure, clean look while maintaining proper chassis alignment.</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1192\" data-end=\"1215\"><strong data-start=\"1192\" data-end=\"1212\">Package Contents</strong>:</p>\r\n<ul data-start=\"1216\" data-end=\"1261\">\r\n<li data-start=\"1216\" data-end=\"1230\">\r\n<p data-start=\"1218\" data-end=\"1230\">(13) Bolts</p>\r\n</li>\r\n<li data-start=\"1231\" data-end=\"1244\">\r\n<p data-start=\"1233\" data-end=\"1244\">(13) Nuts</p>\r\n</li>\r\n<li data-start=\"1245\" data-end=\"1261\">\r\n<p data-start=\"1247\" data-end=\"1261\">(13) Washers</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1263\" data-end=\"1495\"><strong data-start=\"1263\" data-end=\"1278\">Application</strong>:<br data-start=\"1279\" data-end=\"1282\">Install as specified for securing the floor pan to the kart chassis. Designed for <strong data-start=\"1364\" data-end=\"1375\">Charger</strong>, <strong data-start=\"1377\" data-end=\"1389\">Platinum</strong>, <strong data-start=\"1391\" data-end=\"1402\">Prodigy</strong>, and <strong data-start=\"1408\" data-end=\"1425\">Prodigy Cadet</strong> karts. Ideal for new builds, rebuilds, or maintenance replacements.</p>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "floor-pan-bolt-kit",
                                VendorID = chargerId,
                                Tag = "Bodywork, fiberglass, Floor Pans, Platinum, Prodigy, prodigy cadet",
                                Type = "Bodywork"
                            },
                            new Product //11
                            {
                                ProductName = "Nerf Bar Pin Assembly",
                                Description = "<p data-start=\"312\" data-end=\"743\">The <strong data-start=\"335\" data-end=\"360\">Nerf Bar Pin Assembly</strong> from <strong data-start=\"366\" data-end=\"392\">Charger Racing Chassis</strong> is a complete hardware kit designed to secure the left or right nerf bar to your kart chassis. Each assembly is built with durable components to ensure a tight, reliable connection that can withstand the rigors of competitive racing. Ideal for both maintenance and new builds, this assembly makes nerf bar installation fast, secure, and repeatable.</p>\r\n<p data-start=\"745\" data-end=\"764\"><strong data-start=\"745\" data-end=\"762\">Key Features:</strong></p>\r\n<ul data-start=\"765\" data-end=\"1294\">\r\n<li data-start=\"765\" data-end=\"867\">\r\n<p data-start=\"767\" data-end=\"867\"><strong data-start=\"767\" data-end=\"793\">Complete Hardware Kit:</strong> Includes all required components for one nerf bar side (left or right).</p>\r\n</li>\r\n<li data-start=\"868\" data-end=\"973\">\r\n<p data-start=\"870\" data-end=\"973\"><strong data-start=\"870\" data-end=\"901\">High-Strength Construction:</strong> Precision-engineered pins and washers ensure long-lasting durability.</p>\r\n</li>\r\n<li data-start=\"974\" data-end=\"1091\">\r\n<p data-start=\"976\" data-end=\"1091\"><strong data-start=\"976\" data-end=\"1001\">Quick-Release Design:</strong> Safety pins allow for fast nerf bar removal and installation during setup or transport.</p>\r\n</li>\r\n<li data-start=\"1092\" data-end=\"1191\">\r\n<p data-start=\"1094\" data-end=\"1191\"><strong data-start=\"1094\" data-end=\"1112\">Universal Fit:</strong> Compatible with both left and right side nerf bar mounts on Charger chassis.</p>\r\n</li>\r\n<li data-start=\"1192\" data-end=\"1294\">\r\n<p data-start=\"1194\" data-end=\"1294\"><strong data-start=\"1194\" data-end=\"1220\">Race-Ready Durability:</strong> Designed to handle repeated impact and vibration under race conditions.</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1296\" data-end=\"1311\"><strong data-start=\"1296\" data-end=\"1309\">Includes:</strong></p>\r\n<ul data-start=\"1312\" data-end=\"1373\">\r\n<li data-start=\"1312\" data-end=\"1331\">\r\n<p data-start=\"1314\" data-end=\"1331\">(3) Clevis Pins</p>\r\n</li>\r\n<li data-start=\"1332\" data-end=\"1357\">\r\n<p data-start=\"1334\" data-end=\"1357\">(3) Small Safety Pins</p>\r\n</li>\r\n<li data-start=\"1358\" data-end=\"1373\">\r\n<p data-start=\"1360\" data-end=\"1373\">(6) Washers</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1375\" data-end=\"1555\"><strong data-start=\"1375\" data-end=\"1391\">Application:</strong><br data-start=\"1391\" data-end=\"1394\">Used to mount and secure nerf bars on <strong data-start=\"1432\" data-end=\"1458\">Charger Racing Chassis</strong> karts. Recommended for racers replacing worn hardware or assembling new bumpers and side bars.</p>",
                                IsActive = true,
                                CategoryID = catMotorPartsId,
                                Handle = "nerf-bar-pin-assembly",
                                VendorID = chargerId,
                                Tag = "Nerf Bars and Bumpers",
                                Type = "Nerf Bars and Bumpers"
                            },
                            new Product //12
                            {
                                ProductName = "Pitman Arm Bolt Assembly",
                                Description = "<p data-start=\"198\" data-end=\"620\">The <strong data-start=\"221\" data-end=\"249\">Pitman Arm Bolt Assembly</strong> from <strong data-start=\"255\" data-end=\"281\">Charger Racing Chassis</strong> is a precision steering component designed to securely fasten the pitman arm to the steering system, ensuring smooth, consistent, and reliable steering performance. Built from high-quality hardware and engineered for durability, this assembly withstands the rigors of competitive karting and maintains proper steering geometry under load.</p>\r\n<p data-start=\"622\" data-end=\"777\">Perfect for maintenance, rebuilds, and new chassis construction, this bolt assembly provides the secure connection required for accurate steering response.</p>\r\n<p data-start=\"779\" data-end=\"805\"><strong data-start=\"779\" data-end=\"803\">Included Components:</strong></p>\r\n<ul data-start=\"806\" data-end=\"880\">\r\n<li data-start=\"806\" data-end=\"829\">\r\n<p data-start=\"808\" data-end=\"829\">(1) Pitman Arm Bolt</p>\r\n</li>\r\n<li data-start=\"830\" data-end=\"844\">\r\n<p data-start=\"832\" data-end=\"844\">(1) Washer</p>\r\n</li>\r\n<li data-start=\"845\" data-end=\"861\">\r\n<p data-start=\"847\" data-end=\"861\">(1) Lock Nut</p>\r\n</li>\r\n<li data-start=\"862\" data-end=\"880\">\r\n<p data-start=\"864\" data-end=\"880\">(1) Safety Pin</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"882\" data-end=\"1077\"><strong data-start=\"882\" data-end=\"898\">Application:</strong><br data-start=\"898\" data-end=\"901\">Install as specified on Charger steering systems where a pitman arm bolt is required. Ideal for replacing worn hardware or completing steering system assembly on Charger karts.</p>",
                                IsActive = true,
                                CategoryID = catMotorPartsId,
                                Handle = "pitman-arm-bolt-assembly",
                                VendorID = chargerId,
                                Tag = "Steering Components",
                                Type = "Steering Components"
                            },
                            new Product //13
                            {
                                ProductName = "Brake Pedal",
                                Description = "<p>Durable Construction: Designed to withstand the stresses of high-performance kart racing.<br>Reverse Configuration: Ideal for karts with reverse braking systems.<br>Comfortable Design: Provides secure grip for better control during braking.<br>Precision Performance: Ensures consistent braking response in competitive conditions.<br>Ideal For: Motorsport teams and karting enthusiasts needing a reliable brake pedal.</p>",
                                IsActive = true,
                                CategoryID = catMotorPartsId,
                                Handle = "brake-pedal",
                                VendorID = authenticId,
                                Tag = "Brake Controls",
                                Type = "Brakes"
                            },
                            new Product //14
                            {
                                ProductName = "Axle Collar Set",
                                Description = "<p data-start=\"154\" data-end=\"698\"><strong data-start=\"258\" data-end=\"273\">Description</strong>:<br data-start=\"274\" data-end=\"277\">The <strong data-start=\"281\" data-end=\"300\">Axle Collar Set</strong> from <strong data-start=\"306\" data-end=\"332\">Charger Racing Chassis</strong> provides secure positioning for your axle assembly, ensuring stability and proper alignment under high-performance conditions. Each set includes three precision-machined collars designed to hold the axle firmly in place, preventing lateral movement while maintaining smooth rotation. Ideal for assembly, maintenance, and replacement on all Charger chassis models.</p>\r\n<p data-start=\"700\" data-end=\"719\"><strong data-start=\"700\" data-end=\"716\">Key Features</strong>:</p>\r\n<ul data-start=\"720\" data-end=\"1150\">\r\n<li data-start=\"720\" data-end=\"818\">\r\n<p data-start=\"722\" data-end=\"818\"><strong data-start=\"722\" data-end=\"744\">Precision Machined</strong>: Ensures tight tolerance and consistent fit for maximum axle stability.</p>\r\n</li>\r\n<li data-start=\"819\" data-end=\"922\">\r\n<p data-start=\"821\" data-end=\"922\"><strong data-start=\"821\" data-end=\"841\">Three-Collar Set</strong>: Includes three high-quality collars for even pressure and reliable retention.</p>\r\n</li>\r\n<li data-start=\"923\" data-end=\"1041\">\r\n<p data-start=\"925\" data-end=\"1041\"><strong data-start=\"925\" data-end=\"949\">Durable Construction</strong>: Built from hardened steel or aluminum (depending on setup) for long-lasting performance.</p>\r\n</li>\r\n<li data-start=\"1042\" data-end=\"1150\">\r\n<p data-start=\"1044\" data-end=\"1150\"><strong data-start=\"1044\" data-end=\"1070\">Enhanced Axle Security</strong>: Prevents unwanted side-to-side axle movement during intense race conditions.</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1152\" data-end=\"1413\"><strong data-start=\"1152\" data-end=\"1167\">Application</strong>:<br data-start=\"1168\" data-end=\"1171\">Install as specified during axle assembly or service. Confirm axle diameter and proper fitment before installation. Compatible with <strong data-start=\"1303\" data-end=\"1314\">Charger</strong>, <strong data-start=\"1316\" data-end=\"1328\">Platinum</strong>, <strong data-start=\"1330\" data-end=\"1341\">Prodigy</strong>, and <strong data-start=\"1347\" data-end=\"1357\">Legacy</strong> chassis models using standard axle retention systems.</p>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "axle-collar-set",
                                VendorID = authenticId,
                                Tag = "Axles & Components",
                                Type = "Axles & Components"
                            },
                            new Product //15
                            {
                                ProductName = "1/4 - 28 X 1-1/4\" Bullet End Stud",
                                Description = "<p data-start=\"177\" data-end=\"689\">The 1/4 - 28 x 1-1/4\" Bullet End Stud is a precision fastener from <strong data-start=\"400\" data-end=\"426\">Charger Racing Chassis</strong>, designed for reliability and consistency in kart chassis and spindle assemblies. Featuring a bullet-style end for easier alignment during installation, this stud ensures secure fastening, durability, and ease of maintenance in competitive racing environments.</p>\r\n<p data-start=\"691\" data-end=\"710\"><strong data-start=\"691\" data-end=\"707\">Key Features</strong>:</p>\r\n<ul data-start=\"711\" data-end=\"1215\">\r\n<li data-start=\"711\" data-end=\"826\">\r\n<p data-start=\"713\" data-end=\"826\"><strong data-start=\"713\" data-end=\"734\">Bullet End Design</strong>: Simplifies installation by allowing quick thread alignment and reducing cross-threading.</p>\r\n</li>\r\n<li data-start=\"827\" data-end=\"959\">\r\n<p data-start=\"829\" data-end=\"959\"><strong data-start=\"829\" data-end=\"852\">Precision Threading</strong>: The 1/4 - 28 fine thread provides a tight, secure fit to maintain consistent torque and clamping force.</p>\r\n</li>\r\n<li data-start=\"960\" data-end=\"1097\">\r\n<p data-start=\"962\" data-end=\"1097\"><strong data-start=\"962\" data-end=\"986\">Durable Construction</strong>: Manufactured from high-grade materials to resist vibration, impact, and wear in demanding track conditions.</p>\r\n</li>\r\n<li data-start=\"1098\" data-end=\"1215\">\r\n<p data-start=\"1100\" data-end=\"1215\"><strong data-start=\"1100\" data-end=\"1121\">Universal Fitment</strong>: Ideal for use across multiple chassis models, spindle assemblies, and steering components.</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1217\" data-end=\"1567\"><strong data-start=\"1217\" data-end=\"1232\">Application</strong>:<br data-start=\"1233\" data-end=\"1236\">Install as specified for kart chassis or spindle assemblies. Verify correct size and thread pitch before installation. Commonly used in <strong data-start=\"1372\" data-end=\"1382\">Legacy</strong>, <strong data-start=\"1384\" data-end=\"1400\">Legacy Cadet</strong>, <strong data-start=\"1402\" data-end=\"1412\">Magnum</strong>, <strong data-start=\"1414\" data-end=\"1426\">Platinum</strong>, <strong data-start=\"1428\" data-end=\"1439\">Prodigy</strong>, and <strong data-start=\"1445\" data-end=\"1462\">Prodigy Cadet</strong> chassis models, as well as other karts utilizing 1/4 - 28 hardware in spindle and steering components.</p>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "1-4-28-x-1-1-4-bullet-end-stud",
                                VendorID = chargerId,
                                Tag = "1/2 nuts, 1/4-28, 7/16 nuts, Axles & Components, Legacy, legacy cadet, lug nuts, lugs, Magnum, Platinum, Prodigy, prodigy cadet, Spindle Items, Steering Components, Wheel Hubs, wheel nuts",
                                Type = "Spindle Items"
                            },
                            new Product //16
                            {
                                ProductName = "Caster Block Safety Pin Assembly",
                                Description = "<p data-start=\"206\" data-end=\"751\">The <strong data-start=\"367\" data-end=\"403\">Caster Block Safety Pin Assembly</strong> from <strong data-start=\"409\" data-end=\"435\">Charger Racing Chassis</strong> is a precision-engineered safety component designed to secure the caster block assembly and prevent unwanted movement or separation during racing. Built for reliability, this assembly adds an extra layer of security to the front-end setup, ensuring consistent handling and durability under high-stress conditions.</p>\r\n<p data-start=\"753\" data-end=\"772\"><strong data-start=\"753\" data-end=\"769\">Key Features</strong>:</p>\r\n<ul data-start=\"773\" data-end=\"1176\">\r\n<li data-start=\"773\" data-end=\"877\">\r\n<p data-start=\"775\" data-end=\"877\"><strong data-start=\"775\" data-end=\"800\">Complete Hardware Set</strong>: Includes all necessary components for securing the caster block assembly.</p>\r\n</li>\r\n<li data-start=\"878\" data-end=\"966\">\r\n<p data-start=\"880\" data-end=\"966\"><strong data-start=\"880\" data-end=\"899\">Enhanced Safety</strong>: Prevents caster block separation or loosening during operation.</p>\r\n</li>\r\n<li data-start=\"967\" data-end=\"1083\">\r\n<p data-start=\"969\" data-end=\"1083\"><strong data-start=\"969\" data-end=\"993\">Durable Construction</strong>: High-quality steel and tether materials designed for repeated use and easy inspection.</p>\r\n</li>\r\n<li data-start=\"1084\" data-end=\"1176\">\r\n<p data-start=\"1086\" data-end=\"1176\"><strong data-start=\"1086\" data-end=\"1109\">Simple Installation</strong>: Direct-fit design for Charger Racing Chassis front-end systems.</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1178\" data-end=\"1201\"><strong data-start=\"1178\" data-end=\"1198\">Package Contents</strong>:</p>\r\n<ul data-start=\"1202\" data-end=\"1291\">\r\n<li data-start=\"1202\" data-end=\"1234\">\r\n<p data-start=\"1204\" data-end=\"1234\">(2) Caster Block Lower Bolts</p>\r\n</li>\r\n<li data-start=\"1235\" data-end=\"1250\">\r\n<p data-start=\"1237\" data-end=\"1250\">(2) Washers</p>\r\n</li>\r\n<li data-start=\"1251\" data-end=\"1270\">\r\n<p data-start=\"1253\" data-end=\"1270\">(2) Safety Pins</p>\r\n</li>\r\n<li data-start=\"1271\" data-end=\"1291\">\r\n<p data-start=\"1273\" data-end=\"1291\">(2) Tether Wires</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1293\" data-end=\"1619\"><strong data-start=\"1293\" data-end=\"1308\">Application</strong>:<br data-start=\"1309\" data-end=\"1312\">Install as specified to secure caster block assemblies on Charger Racing Chassis front-end systems. Confirm correct bolt sizing and fitment before installation. Compatible with <strong data-start=\"1489\" data-end=\"1500\">Prodigy</strong>, <strong data-start=\"1502\" data-end=\"1512\">Legacy</strong>, <strong data-start=\"1514\" data-end=\"1524\">Magnum</strong>, and other Charger chassis utilizing standard front spindle and caster block configurations.</p>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "caster-block-safety-pin-assembly",
                                VendorID = chargerId,
                                Tag = "Camber Components, Caster Components, spindle, Spindle Items, Steering Components",
                                Type = "Steering Components"
                            },
                            new Product //17
                            {
                                ProductName = "Lightweight Aluminum 5/8\" Spindle Nut",
                                Description = "<p data-start=\"352\" data-end=\"832\">The <strong data-start=\"375\" data-end=\"424\">Lightweight Aluminum 5/8\" Spindle Nut Set (2)</strong> from <strong data-start=\"430\" data-end=\"456\">Charger Racing Chassis</strong> is designed for racers seeking weight savings without sacrificing strength or reliability. Precision-machined from high-grade aluminum, these spindle nuts reduce rotating mass and improve front-end response, making them ideal for competitive karting applications. Their durable silver anodized finish provides both corrosion resistance and a clean, professional appearance.</p>\r\n<p data-start=\"834\" data-end=\"853\"><strong data-start=\"834\" data-end=\"851\">Key Features:</strong></p>\r\n<ul data-start=\"854\" data-end=\"1344\">\r\n<li data-start=\"854\" data-end=\"968\">\r\n<p data-start=\"856\" data-end=\"968\"><strong data-start=\"856\" data-end=\"879\">Lightweight Design:</strong> Aluminum construction reduces unsprung weight for improved handling and steering feel.</p>\r\n</li>\r\n<li data-start=\"969\" data-end=\"1069\">\r\n<p data-start=\"971\" data-end=\"1069\"><strong data-start=\"971\" data-end=\"994\">Precision Machined:</strong> Ensures a tight, accurate fit for reliable spindle assembly performance.</p>\r\n</li>\r\n<li data-start=\"1070\" data-end=\"1166\">\r\n<p data-start=\"1072\" data-end=\"1166\"><strong data-start=\"1072\" data-end=\"1103\">Corrosion-Resistant Finish:</strong> Silver anodized coating protects against oxidation and wear.</p>\r\n</li>\r\n<li data-start=\"1167\" data-end=\"1248\">\r\n<p data-start=\"1169\" data-end=\"1248\"><strong data-start=\"1169\" data-end=\"1184\">Set of Two:</strong> Supplied as a matched pair for complete spindle installation.</p>\r\n</li>\r\n<li data-start=\"1249\" data-end=\"1344\">\r\n<p data-start=\"1251\" data-end=\"1344\"><strong data-start=\"1251\" data-end=\"1278\">Race-Proven Durability:</strong> Built to withstand the rigors of speedway and oval kart racing.</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1346\" data-end=\"1557\"><strong data-start=\"1346\" data-end=\"1362\">Application:</strong><br data-start=\"1362\" data-end=\"1365\">Used to secure front spindle assemblies on <strong data-start=\"1408\" data-end=\"1419\">Charger</strong>, <strong data-start=\"1421\" data-end=\"1432\">Prodigy</strong>, and <strong data-start=\"1438\" data-end=\"1450\">Platinum</strong> chassis. Ideal for racers upgrading from standard steel nuts to lighter, performance-focused components.</p>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "lightweight-aluminum-5-8-spindle-nut",
                                VendorID = chargerId,
                                Tag = "Spindle Items",
                                Type = "Spindle Items"
                            },
                            new Product //18
                            {
                                ProductName = "Steering Yoke with Nut",
                                Description = "<p data-start=\"307\" data-end=\"790\">The Steering Yoke with Nut by <strong data-start=\"356\" data-end=\"382\">Charger Racing Chassis</strong> is a threaded steering linkage component designed to provide a secure and adjustable connection within the kart steering system. Precision-machined for consistent fitment, this yoke assembly allows accurate steering alignment while maintaining strength under racing loads. Supplied with the matching retaining nut, it offers a complete and service-ready solution for steering system assembly or replacement.</p>\r\n<p data-start=\"792\" data-end=\"1133\"><strong data-start=\"792\" data-end=\"808\">Application:</strong><br data-start=\"808\" data-end=\"811\">Used as part of the steering linkage on Charger chassis platforms to connect steering components and fine-tune steering geometry. Ideal for new builds, steering repairs, or replacing worn threaded yokes. Suitable for competitive karting applications where reliable steering response and adjustment capability are required.</p>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "steering-yoke-with-nut",
                                VendorID = chargerId,
                                Tag = "Steering Components",
                                Type = "Steering Components"
                            },
                            new Product //19
                            {
                                ProductName = "Quick Release 5/8\" Steering Wheel Hub for Champ",
                                Description = "<p data-start=\"270\" data-end=\"733\">The Quick Release 5/8\" Steering Wheel Hub for Champ from Charger Racing Chassis is a precision-engineered steering component designed to deliver fast, secure, and reliable wheel removal during kart setup, transport, and maintenance. Built to withstand demanding race environments, this hub provides a smooth locking action and consistent performance, ensuring the wheel stays firmly in place while still offering effortless release when needed.</p>\r\n<p data-start=\"735\" data-end=\"902\">Constructed with durable, race-grade materials, itâ€™s an essential upgrade for drivers and teams seeking convenience, safety, and efficiency in Champ kart applications.</p>\r\n<p data-start=\"904\" data-end=\"923\"><strong data-start=\"904\" data-end=\"921\">Key Features:</strong></p>\r\n<ul data-start=\"924\" data-end=\"1392\">\r\n<li data-start=\"924\" data-end=\"1085\">\r\n<p data-start=\"926\" data-end=\"1085\"><strong data-start=\"926\" data-end=\"953\">Quick-Release Function:</strong> Allows rapid removal and attachment of the steering wheelâ€”ideal for pit adjustments, driver changes, or tight cockpit entry/exit.</p>\r\n</li>\r\n<li data-start=\"1086\" data-end=\"1181\">\r\n<p data-start=\"1088\" data-end=\"1181\"><strong data-start=\"1088\" data-end=\"1105\">5/8\" Fitment:</strong> Designed specifically for 5/8\" steering shafts used on Champ-style karts.</p>\r\n</li>\r\n<li data-start=\"1182\" data-end=\"1281\">\r\n<p data-start=\"1184\" data-end=\"1281\"><strong data-start=\"1184\" data-end=\"1207\">Precision-Machined:</strong> Ensures a tight, reliable connection with smooth engagement every time.</p>\r\n</li>\r\n<li data-start=\"1282\" data-end=\"1392\">\r\n<p data-start=\"1284\" data-end=\"1392\"><strong data-start=\"1284\" data-end=\"1309\">Durable Construction:</strong> Built to endure high-stress racing conditions without loosening or premature wear.</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1394\" data-end=\"1544\"><strong data-start=\"1394\" data-end=\"1410\">Application:</strong><br data-start=\"1410\" data-end=\"1413\">Install as specified for Champ kart steering systems. Ensure proper shaft size and steering wheel bolt pattern before installation.</p>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "quick-release-5-8-steering-wheel-hub-for-champ",
                                VendorID = chargerId,
                                Tag = "Steering Components",
                                Type = "Steering Components"
                            },
                            new Product //20
                            {
                                ProductName = "Lower Steering Upright Bolt Assembly",
                                Description = "<p data-start=\"372\" data-end=\"828\">The <strong data-start=\"395\" data-end=\"435\">Lower Steering Upright Bolt Assembly</strong> from <strong data-start=\"441\" data-end=\"467\">Charger Racing Chassis</strong> is a complete, race-ready hardware set designed to secure the Steering shaft to the tie rods. Each component is precision-manufactured to maintain alignment and ensure reliable steering performance under high-stress racing conditions. The included drilled Allen bolt allows for the use of safety wire, providing additional security during competition.</p>\r\n<p data-start=\"830\" data-end=\"849\"><strong data-start=\"830\" data-end=\"847\">Key Features:</strong></p>\r\n<ul data-start=\"850\" data-end=\"1318\">\r\n<li data-start=\"850\" data-end=\"946\">\r\n<p data-start=\"852\" data-end=\"946\"><strong data-start=\"852\" data-end=\"878\">Complete Hardware Set:</strong> Contains all required components for secure upright installation.</p>\r\n</li>\r\n<li data-start=\"947\" data-end=\"1026\">\r\n<p data-start=\"949\" data-end=\"1026\"><strong data-start=\"949\" data-end=\"977\">Drilled for Safety Wire:</strong> Prevents loosening due to vibration or impact.</p>\r\n</li>\r\n<li data-start=\"1027\" data-end=\"1132\">\r\n<p data-start=\"1029\" data-end=\"1132\"><strong data-start=\"1029\" data-end=\"1058\">Precision-Engineered Fit:</strong> Designed specifically for <strong data-start=\"1085\" data-end=\"1111\">Charger Racing Chassis</strong> front-end systems.</p>\r\n</li>\r\n<li data-start=\"1133\" data-end=\"1223\">\r\n<p data-start=\"1135\" data-end=\"1223\"><strong data-start=\"1135\" data-end=\"1160\">Durable Construction:</strong> Made from high-strength materials for long-term reliability.</p>\r\n</li>\r\n<li data-start=\"1224\" data-end=\"1318\">\r\n<p data-start=\"1226\" data-end=\"1318\"><strong data-start=\"1226\" data-end=\"1249\">Race-Proven Design:</strong> Ensures consistent steering performance and safety in competition.</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1320\" data-end=\"1342\"><strong data-start=\"1320\" data-end=\"1340\">Included in Kit:</strong></p>\r\n<ul data-start=\"1343\" data-end=\"1415\">\r\n<li data-start=\"1343\" data-end=\"1369\">\r\n<p data-start=\"1345\" data-end=\"1369\">(1) Drilled Allen Bolt</p>\r\n</li>\r\n<li data-start=\"1370\" data-end=\"1384\">\r\n<p data-start=\"1372\" data-end=\"1384\">(1) Washer</p>\r\n</li>\r\n<li data-start=\"1385\" data-end=\"1396\">\r\n<p data-start=\"1387\" data-end=\"1396\">(1) Nut</p>\r\n</li>\r\n<li data-start=\"1397\" data-end=\"1415\">\r\n<p data-start=\"1399\" data-end=\"1415\">(1) Safety Pin</p>\r\n</li>\r\n</ul>\r\n<p data-start=\"1417\" data-end=\"1643\"><strong data-start=\"1417\" data-end=\"1433\">Application:</strong><br data-start=\"1433\" data-end=\"1436\">Used to fasten the steering shaft to the tie rods on <strong data-start=\"1490\" data-end=\"1516\">Charger Racing Chassis</strong> karts. Recommended for steering rebuilds, chassis maintenance, or OEM-spec assembly where precision and safety are critical.</p>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "single-steering-upright-bolt-assembly",
                                VendorID = chargerId,
                                Tag = "Steering Components",
                                Type = "Steering Components"
                            },
                            new Product //21
                            {
                                ProductName = "Wheel Hub Spacer",
                                Description = "<p data-start=\"285\" data-end=\"715\">The Wheel Hub Spacer by <strong data-start=\"328\" data-end=\"354\">Charger Racing Chassis</strong> is a precision-machined spacing component designed to fine-tune wheel hub positioning on the spindle. Manufactured for consistent thickness and reliable fitment, this spacer allows accurate adjustment while maintaining proper bearing alignment and smooth wheel rotation. Its durable construction makes it suitable for repeated service and race-day adjustments.</p>\r\n<p data-start=\"717\" data-end=\"1019\"><strong data-start=\"717\" data-end=\"733\">Application:</strong><br data-start=\"733\" data-end=\"736\">Used on front spindle assemblies to space wheel hubs as needed for alignment, clearance, or setup tuning. Ideal for chassis setup changes, maintenance, or replacing worn spacers. Compatible with 5/8\" spindle applicationsâ€”verify thickness and fitment requirements before installation.</p>",
                                IsActive = true,
                                CategoryID = catUncategorizedId,
                                Handle = "1-4-x-5-8-wheel-hub-spacer",
                                VendorID = chargerId,
                                Tag = "Spindle Items",
                                Type = "Spindle Items"
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
                                UnitPrice = 99.99m,
                                StockQuantity = 20,
                                SKU_ExternalID = "URW-001",
                                InventoryPolicy = InventoryPolicy.Continue,
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Size", Value = "Small" },
                                        new Variant { Name = "Color", Value = "Red" }
                                    }
                            },
                                new ProductVariant //1
                                {
                                    ProductId = 1,
                                    UnitPrice = 109.99m,
                                    StockQuantity = 15,
                                    SKU_ExternalID = "URW-002",
                                    InventoryPolicy = InventoryPolicy.Continue,
                                    Status = true,
                                    Options = new List<Variant>
                                    {
                                        new Variant { Name = "Size", Value = "Small" },
                                        new Variant { Name = "Color", Value = "Blue" }
                                    }
                                },
                                new ProductVariant //1
                                {
                                    ProductId = 1,
                                    UnitPrice = 119.99m,
                                    StockQuantity = 10,
                                    SKU_ExternalID = "URW-003",
                                    InventoryPolicy = InventoryPolicy.Deny,
                                    Status = true,
                                    Options = new List<Variant>
                                    {
                                        new Variant { Name = "Size", Value = "Large" },
                                        new Variant { Name = "Color", Value = "Red" }
                                    }
                                },
                                new ProductVariant //1
                                {
                                    ProductId = 1,
                                    UnitPrice = 129.99m,
                                    StockQuantity = 5,
                                    SKU_ExternalID = "URW-004",
                                    InventoryPolicy = InventoryPolicy.Deny,
                                    Status = false,
                                    Options = new List<Variant>
                                    {
                                        new Variant { Name = "Size", Value = "Large" },
                                        new Variant { Name = "Color", Value = "Blue" }
                                    }
                                },
                            new ProductVariant //2
                            {
                                ProductId = 2,
                                UnitPrice = 33.90m,
                                StockQuantity = 20,
                                SKU_ExternalID = "1170",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 453.59237m,
                                Barcode = "TP1170",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Type", Value = "Throttle Pedal" }
                                    }
                            },
                            new ProductVariant //2
                            {
                                ProductId = 2,
                                UnitPrice = 39.55m,
                                StockQuantity = 22,
                                SKU_ExternalID = "1171",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 453.59237m,
                                Barcode = "TP1171",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Type", Value = "Reverse Throttle Pedal" }
                                    }
                            },
                            new ProductVariant //2
                            {
                                ProductId = 2,
                                UnitPrice = 33.90m,
                                StockQuantity = 22,
                                SKU_ExternalID = "1182",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 453.59237m,
                                Barcode = "BP1182",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Type", Value = "Brake Pedal" }
                                    }
                            },
                            new ProductVariant //2
                            {
                                ProductId = 2,
                                UnitPrice = 39.55m,
                                StockQuantity = 22,
                                SKU_ExternalID = "1183",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 453.59237m,
                                Barcode = "BP1183",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Type", Value = "Reverse Brake Pedal" }
                                    }
                            },
                            new ProductVariant //3
                            {
                                ProductId = 3,
                                UnitPrice = 14.3m,
                                StockQuantity = 42,
                                SKU_ExternalID = "1075",
                                InventoryPolicy = InventoryPolicy.Continue,
                                Weight = 453.59237m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Type", Value = "6 1/2\" Tie Rod Left" }
                                    }
                            },
                            new ProductVariant //3
                            {
                                ProductId = 3,
                                UnitPrice = 14.3m,
                                StockQuantity = 31,
                                SKU_ExternalID = "1074",
                                InventoryPolicy = InventoryPolicy.Continue,
                                Weight = 453.59237m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Type", Value = "11 3/4\" Tie Rod Right" }
                                    }
                            },
                            new ProductVariant //4
                            {
                                ProductId = 4,
                                UnitPrice = 37.29m,
                                StockQuantity = 12,
                                SKU_ExternalID = "1092",
                                InventoryPolicy = InventoryPolicy.Continue,
                                Weight = 907.18474m,
                                Barcode = "TL1092J",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                            new ProductVariant //5
                            {
                                ProductId = 5,
                                UnitPrice = 0.77m,
                                StockQuantity = 7,
                                SKU_ExternalID = "1100",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 113.3980925m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Size", Value = "'-.060\" Thick" }
                                    }
                            },
                            new ProductVariant //5
                            {
                                ProductId = 5,
                                UnitPrice = 0.77m,
                                StockQuantity = 11,
                                SKU_ExternalID = "1101",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 113.3980925m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Size", Value = "'-.030\" Thin" }
                                    }
                            },
                             new ProductVariant //6
                             {
                                 ProductId = 6,
                                 UnitPrice = 79.1m,
                                 StockQuantity = 20,
                                 SKU_ExternalID = "3fuel",
                                 InventoryPolicy = InventoryPolicy.Deny,
                                 Weight = 453.59237m,
                                 Barcode = "",
                                 Status = true,
                                 Options = new List<Variant>
                                    {
                                        new Variant { Name = "Size", Value = "3 Qt" }
                                    }
                             },
                            new ProductVariant //6
                            {
                                ProductId = 6,
                                UnitPrice = 84.75m,
                                StockQuantity = 10,
                                SKU_ExternalID = "5fuel",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 907.1847m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Size", Value = "5 Qt" }
                                    }
                            },
                            new ProductVariant //6
                            {
                                ProductId = 6,
                                UnitPrice = 169.5m,
                                StockQuantity = 23,
                                SKU_ExternalID = "7fuel",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 3175.14659m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Size", Value = "7 Qt Floor Mounted" }
                                    }
                            },
                            new ProductVariant //6
                            {
                                ProductId = 6,
                                UnitPrice = 28.25m,
                                StockQuantity = 3,
                                SKU_ExternalID = "2fuel",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 453.59237m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Size", Value = "2 Qt Floor Mounted" }
                                    }
                            },
                            new ProductVariant //7
                            {
                                ProductId = 7,
                                UnitPrice = 13.56m,
                                StockQuantity = 19,
                                InventoryPolicy = InventoryPolicy.Deny,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                            new ProductVariant //8
                            {
                                ProductId = 8,
                                UnitPrice = 31.92m,
                                StockQuantity = 27,
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 453.59237m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                            new ProductVariant //9
                            {
                                ProductId = 9,
                                UnitPrice = 202.5m,
                                StockQuantity = 21,
                                SKU_ExternalID = "AiM-X05SNRP972",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Barcode = "52718556",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                            new ProductVariant //10
                            {
                                ProductId = 10,
                                UnitPrice = 7.15m,
                                StockQuantity = 20,
                                SKU_ExternalID = "1223",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 45.359237m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                            new ProductVariant //11
                            {
                                ProductId = 11,
                                UnitPrice = 8.19m,
                                StockQuantity = 33,
                                SKU_ExternalID = "1070",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 226.796185m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                            new ProductVariant //12
                            {
                                ProductId = 12,
                                UnitPrice = 6.5m,
                                StockQuantity = 28,
                                SKU_ExternalID = "1085",
                                InventoryPolicy = InventoryPolicy.Continue,
                                Weight = 226.796185m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                            new ProductVariant //13
                            {
                                ProductId = 13,
                                UnitPrice = 44.55m,
                                StockQuantity = 19,
                                SKU_ExternalID = "PRC-1125100",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Barcode = "24943068",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Option", Value = "Regular" }
                                    }
                            },
                            new ProductVariant //13
                            {
                                ProductId = 13,
                                UnitPrice = 44.55m,
                                StockQuantity = 31,
                                SKU_ExternalID = "PRC-1125100R",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Barcode = "18094044",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Option", Value = "Reverse" }
                                    }
                            },
                            new ProductVariant //14
                            {
                                ProductId = 14,
                                UnitPrice = 27.12m,
                                StockQuantity = 7,
                                SKU_ExternalID = "1153",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 453.59237m,
                                Barcode = "AX1153",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                            new ProductVariant //15
                            {
                                ProductId = 15,
                                UnitPrice = 2.26m,
                                StockQuantity = 27,
                                SKU_ExternalID = "1147",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 113.3980925m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                            new ProductVariant //16
                            {
                                ProductId = 16,
                                UnitPrice = 15.59m,
                                StockQuantity = 18,
                                SKU_ExternalID = "1137",
                                InventoryPolicy = InventoryPolicy.Continue,
                                Weight = 226.796185m,
                                Barcode = "CA1137",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                            new ProductVariant //17
                            {
                                ProductId = 17,
                                UnitPrice = 11.3m,
                                StockQuantity = 22,
                                SKU_ExternalID = "1106",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 113.3980925m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Color", Value = "Silver" }
                                    }
                            },
                            new ProductVariant //18
                            {
                                ProductId = 18,
                                UnitPrice = 16.89m,
                                StockQuantity = 39,
                                SKU_ExternalID = "1095",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 453.59237m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                            new ProductVariant //19
                            {
                                ProductId = 19,
                                UnitPrice = 57.18m,
                                StockQuantity = 9,
                                SKU_ExternalID = "1091",
                                InventoryPolicy = InventoryPolicy.Continue,
                                Weight = 2267.96185m,
                                Barcode = "QR1091L",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                            new ProductVariant //20
                            {
                                ProductId = 20,
                                UnitPrice = 5.65m,
                                StockQuantity = 28,
                                SKU_ExternalID = "1086",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 113.3980925m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Title", Value = "Default" }
                                    }
                            },
                             new ProductVariant //21
                             {
                                 ProductId = 21,
                                 UnitPrice = 1.25m,
                                 StockQuantity = 11,
                                 SKU_ExternalID = "1109",
                                 InventoryPolicy = InventoryPolicy.Deny,
                                 Weight = 113.3980925m,
                                 Barcode = "",
                                 Status = true,
                                 Options = new List<Variant>
                                    {
                                        new Variant { Name = "Axle Size", Value = "1/8 thick x 5/8" },
                                    }
                             },
                            new ProductVariant //21
                            {
                                ProductId = 21,
                                UnitPrice = 1.25m,
                                StockQuantity = 6,
                                SKU_ExternalID = "1110",
                                InventoryPolicy = InventoryPolicy.Deny,
                                Weight = 113.3980925m,
                                Barcode = "",
                                Status = true,
                                Options = new List<Variant>
                                    {
                                        new Variant { Name = "Axle Size", Value = "18 thick x3/4" },
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
