using System.IO;

namespace demo.Sqlite;

public static class FactoryExtension
{
    public static void initailize(this FactoryContext context)
    {
        if (File.Exists(context.DbFilePath) == false)
        {
            context.Database.EnsureCreated();

            // 재고 현황 예제값 추가
            context.Inventories.Add(new Inventory() { Id = 1, ItemName = "마우스", LotNumber = "2025-04", Quantity = 100, WhsName = "서울" });
            context.Inventories.Add(new Inventory() { Id = 2, ItemName = "키보드", LotNumber = "2025-04", Quantity = 50, WhsName = "서울" });
            context.Inventories.Add(new Inventory() { Id = 3, ItemName = "모니터", LotNumber = "2025-04", Quantity = 10, WhsName = "서울" });
            context.Inventories.Add(new Inventory() { Id = 4, ItemName = "마우스", LotNumber = "2025-03", Quantity = 100, WhsName = "부산" });
            context.Inventories.Add(new Inventory() { Id = 5, ItemName = "키보드", LotNumber = "2025-03", Quantity = 100, WhsName = "대구" });

            // 영업 현황 예제값 추가
            context.SalesOrders.Add(new SalesOrder { CustName = "김진석", ItemName = "마우스", Quantity = 10, DueDate = new DateTime(2025, 4, 3) });
            context.SalesOrders.Add(new SalesOrder { CustName = "김진석", ItemName = "키보드", Quantity = 2, DueDate = new DateTime(2025, 4, 3) });
            context.SalesOrders.Add(new SalesOrder { CustName = "김진석", ItemName = "모니터", Quantity = 10, DueDate = new DateTime(2025, 4, 3) });
            context.SalesOrders.Add(new SalesOrder { CustName = "박구삼", ItemName = "마우스", Quantity = 10, DueDate = new DateTime(2025, 4, 4) });
            context.SalesOrders.Add(new SalesOrder { CustName = "박구삼", ItemName = "키보드", Quantity = 10, DueDate = new DateTime(2025, 4, 4) });
            context.SalesOrders.Add(new SalesOrder { CustName = "김진석", ItemName = "마우스", Quantity = 10, DueDate = new DateTime(2025, 4, 5) });
            context.SalesOrders.Add(new SalesOrder { CustName = "유저스틴", ItemName = "키보드", Quantity = 10, DueDate = new DateTime(2025, 4, 5) });
            context.SalesOrders.Add(new SalesOrder { CustName = "이종인", ItemName = "모니터", Quantity = 10, DueDate = new DateTime(2025, 4, 5) });

            context.SaveChanges();
        }
    }
}

