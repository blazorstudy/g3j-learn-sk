using System.ComponentModel;

using demo.Sqlite;

using Microsoft.SemanticKernel;

namespace demo.SemanticKernel;

public class InventoryPlugin
{
    [KernelFunction("get_total_count")]
    [Description("전체 품목의 개수를 구하는 함수")]
    [return: Description("전체 품목의 총합계")]
    public int getTotalCount()
    {
        using (var db = new FactoryContext())
        {
            return db.Inventories.Sum(x => x.Quantity);
        }
    }

    [KernelFunction("get_count_by_item_name")]
    [Description("품목별 재고수량을 구하는 기능")]
    [return: Description("특정 품목의 재고수량 합계")]
    public int getCountByItem([Description("품목이름")] string itemname)
    {
        using (var db = new FactoryContext())
        {
            return db.Inventories.Where(x => x.ItemName == itemname).Sum(x => x.Quantity);
        }
    }

    [KernelFunction("get_item_list_of_warehouse")]
    [Description("창고에 있는 품목 정보를 조회")]
    [return: Description("품목 정보")]
    public List<Inventory> getItemListByWhsName([Description("창고이름")] string whsname)
    {
        using (var db = new FactoryContext())
        {
            return db.Inventories.Where(x => x.WhsName == whsname).ToList();
        }
    }
}
