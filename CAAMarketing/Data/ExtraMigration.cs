using Microsoft.EntityFrameworkCore.Migrations;

namespace CAAMarketing.Data
{
    public static class ExtraMigration
    {
        public static void Steps(MigrationBuilder migrationBuilder)
        {
            //For Category table
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetCategoryTimestampOnUpdate
                    AFTER UPDATE ON Category
                    BEGIN
                        UPDATE Category
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetCategoryTimestampOnInsert
                    AFTER INSERT ON Category
                    BEGIN
                        UPDATE Category
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            //For Employee table
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetEmployeeTimestampOnUpdate
                    AFTER UPDATE ON Employees
                    BEGIN
                        UPDATE Employees
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetEmployeeTimestampOnInsert
                    AFTER INSERT ON Employees
                    BEGIN
                        UPDATE Employees
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            //For Equipements table
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetEquipmentTimestampOnUpdate
                    AFTER UPDATE ON Equipments
                    BEGIN
                        UPDATE Equipments
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetEquipmentTimestampOnInsert
                    AFTER INSERT ON Equipments
                    BEGIN
                        UPDATE Equipments
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            //For Events table
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetEventTimestampOnUpdate
                    AFTER UPDATE ON Events
                    BEGIN
                        UPDATE Events
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetEventTimestampOnInsert
                    AFTER INSERT ON Events
                    BEGIN
                        UPDATE Events
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            //For Inventories table
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetInventoryTimestampOnUpdate
                    AFTER UPDATE ON Inventories
                    BEGIN
                        UPDATE Inventories
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetInventoryeTimestampOnInsert
                    AFTER INSERT ON Inventories
                    BEGIN
                        UPDATE Inventories
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            //For InventoryTransfers table
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetInventoryTransferTimestampOnUpdate
                    AFTER UPDATE ON InventoryTransfers
                    BEGIN
                        UPDATE InventoryTransfers
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetInventoryTransferTimestampOnInsert
                    AFTER INSERT ON InventoryTransfers
                    BEGIN
                        UPDATE InventoryTransfers
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            //For ItemEvents table
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetItemEventTimestampOnUpdate
                    AFTER UPDATE ON ItemEvents
                    BEGIN
                        UPDATE ItemEvents
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetItemEventTimestampOnInsert
                    AFTER INSERT ON ItemEvents
                    BEGIN
                        UPDATE ItemEvents
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");

            //For Items table
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetItemTimestampOnUpdate
                    AFTER UPDATE ON Items
                    BEGIN
                        UPDATE Items
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetItemTimestampOnInsert
                    AFTER INSERT ON Items
                    BEGIN
                        UPDATE Items
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            //For Locations table
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetLocationTimestampOnUpdate
                    AFTER UPDATE ON Locations
                    BEGIN
                        UPDATE Locations
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetLocationTimestampOnInsert
                    AFTER INSERT ON Locations
                    BEGIN
                        UPDATE Locations
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            //For Orders table
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetOrderTimestampOnUpdate
                    AFTER UPDATE ON Orders
                    BEGIN
                        UPDATE Orders
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetOrderTimestampOnInsert
                    AFTER INSERT ON Orders
                    BEGIN
                        UPDATE Orders
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            //For Subscription table
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetSubscriptionTimestampOnUpdate
                    AFTER UPDATE ON Subscriptions
                    BEGIN
                        UPDATE Subscriptions
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetSubscriptionTimestampOnInsert
                    AFTER INSERT ON Subscriptions
                    BEGIN
                        UPDATE Subscriptions
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            //For Suppliers table
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetSupplierTimestampOnUpdate
                    AFTER UPDATE ON Suppliers
                    BEGIN
                        UPDATE Suppliers
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            migrationBuilder.Sql(
                @"
                    CREATE TRIGGER SetSupplierTimestampOnInsert
                    AFTER INSERT ON Suppliers
                    BEGIN
                        UPDATE Suppliers
                        SET RowVersion = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
        }
    }
}
