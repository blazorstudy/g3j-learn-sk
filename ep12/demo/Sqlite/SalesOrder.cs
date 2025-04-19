namespace demo.Sqlite;

using System.ComponentModel.DataAnnotations.Schema;

public class SalesOrder
{
    /// <summary>
    /// 오더ID
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 품목이름
    /// </summary>
    public string ItemName { get; set; }
    /// <summary>
    /// 고객이름
    /// </summary>
    public string CustName { get; set; }
    /// <summary>
    /// 납품수량
    /// </summary>
    public int Quantity { get; set; }
    /// <summary>
    /// 납기일
    /// </summary>
    public DateTime DueDate { get; set; }

    [NotMapped]
    public string TextDueDate => DueDate.ToString("yyyy-MM-dd");
}

