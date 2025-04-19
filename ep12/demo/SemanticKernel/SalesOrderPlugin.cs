using System.ComponentModel;

using demo.Sqlite;

using Microsoft.SemanticKernel;

namespace demo.SemanticKernel;

internal class SalesOrderPlugin
{
    [KernelFunction("get_salesorder_period")]
    [Description("주어진 기간사이에 있는 영업 주문을 조회하는 함수")]
    [return: Description("조회한 영업 주문 목록")]
    public List<SalesOrder> getSalesOrder([Description("조회 시작 일자")] DateTime? start = null, [Description("조회 마지막 일자")] DateTime? end = null)
    {
        start = start ?? DateTime.MinValue;
        end = end ?? DateTime.MaxValue;

        using (var db = new FactoryContext())
        {
            return db.SalesOrders.Where(x => start <= x.DueDate && x.DueDate <= end).ToList();
        }
    }

    [KernelFunction("get_salesorder_date")]
    [Description("특정날짜의 영업 주문 목록을 조회하는 함수")]
    [return: Description("조회한 영업 주문 목록")]
    public List<SalesOrder> getSalesOrderByDate([Description("조회 날짜")] DateTime? day = null )
    {
        day = day ?? DateTime.Today;

        using (var db = new FactoryContext())
        {
            return db.SalesOrders.Where(x => day == x.DueDate).ToList();
        }
    }

    [KernelFunction("get_salesorder_lastest")]
    [Description("최근 며칠간의 영업 주문을 조회하는 함수")]
    [return: Description("조회한 영업 주문 목록")]
    public List<SalesOrder> getLastestSalesOrder([Description("날짜수, 오늘부터터 며칠 전을 나타냄")] int days)
    {
        DateTime start = DateTime.Today.AddDays(days * -1);
        DateTime end = DateTime.Today;

        using (var db = new FactoryContext())
        {
            return db.SalesOrders.Where(x => start <= x.DueDate && x.DueDate <= end).ToList();
        }
    }

    [KernelFunction("get_order_by_customer")]
    [Description("특정한 날짜와 고객이름을 기준으로 영업 주문을 조회")]
    [return: Description("조회한 영업 주문 목록")]
    public List<SalesOrder> getSalesOrderByCustomer([Description("조회하고 싶은 날짜")] DateTime? date, [Description("고객이름")] string custName)
    {
        DateTime orderDate = date ?? DateTime.Today;

        using (var db = new FactoryContext())
        {
            return db.SalesOrders.Where(x => orderDate == x.DueDate && x.CustName == custName)
                                                        .OrderByDescending(x => x.DueDate)
                                                        .ToList();
        }
    }
}
