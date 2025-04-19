using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using demo.SemanticKernel;
using demo.Sqlite;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;

namespace demo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string DeployName { get; set; }
    private string EndPoint { get; set; }
    private string Key { get; set; }
    private string Model { get; set; }

    public MainWindow()
    {
        InitializeComponent();

        setBasicData();

        #region Configuration
        IConfiguration config = new ConfigurationBuilder()
                                      .AddUserSecrets<MainWindow>()
                                      .Build();

        DeployName = config["AOAI:DeployName"];
        EndPoint = config["AOAI:EndPoint"];
        Key = config["AOAI:Key"];
        Model = config["AOAI:Model"];

        Debug.WriteLine($"DeployName: {DeployName}");
        Debug.WriteLine($"EndPoint: {EndPoint}");
        Debug.WriteLine($"Key: {Key}");
        Debug.WriteLine($"Model: {Model}");

        #endregion
    }

    private void setBasicData()
    {
        using (var context = new FactoryContext())
        {
            context.initailize();
            gridStocks.ItemsSource = context.Inventories.ToList();
            gridSales.ItemsSource = context.SalesOrders.ToList();
        }
    }

    private async void btnSend_Click(object sender, RoutedEventArgs e)
    {
        string query = txtPrompt.Text;

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(DeployName, EndPoint, Key, Model);
        var kernel = builder.Build();

        kernel.PromptRenderFilters.Add(new PromptFilter());
        kernel.ImportPluginFromType<InventoryPlugin>("InventoryPlugin");
        kernel.ImportPluginFromType<SalesOrderPlugin>("SalesOrderPlugin");

        OpenAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
        KernelArguments arguments = new(settings);
        var response = await kernel.InvokePromptAsync(query, arguments);

        txtBox.Text = $"{response}";

    }

    private void btnAddStock_Click(object sender, RoutedEventArgs e)
    {
        using (var db = new FactoryContext())
        {
            db.Inventories.Add(new Inventory { ItemName = txtItemName.Text, LotNumber = txtLotNumber.Text, Quantity = int.Parse(txtQuantity.Text), WhsName = txtWhsName.Text });
            db.SaveChanges();

            gridStocks.ItemsSource = db.Inventories.ToList();
        }
    }

    private void btnDelStock_Click(object sender, RoutedEventArgs e)
    {
        using (var db = new FactoryContext())
        {
            var i = db.Inventories.FirstOrDefault(x => x.Id == int.Parse(txtId.Text));
            if (i == null)
                return;

            db.Inventories.Remove(i);
            db.SaveChanges();

            gridStocks.ItemsSource = db.Inventories.ToList();
        }
    }

    private void btnAddSalesOrder_Click(object sender, RoutedEventArgs e)
    {
        using (var db = new FactoryContext())
        {
            db.SalesOrders.Add(new SalesOrder { CustName = txtSalesOrderCustName.Text, ItemName = txtSalesOrderItemName.Text, Quantity = int.Parse(txtSalesOrderQty.Text), DueDate = DateTime.Parse(txtSalesOrderDueDate.Text) });
            db.SaveChanges();

            gridSales.ItemsSource = db.SalesOrders.ToList();
        }
    }

    private void btnDelSalesOrder_Click(object sender, RoutedEventArgs e)
    {
        using (var db = new FactoryContext())
        {
            var i = db.SalesOrders.FirstOrDefault(x => x.Id == int.Parse(txtSalesOrderId.Text));
            if (i == null)
                return;

            db.SalesOrders.Remove(i);
            db.SaveChanges();

            gridSales.ItemsSource = db.SalesOrders.ToList();
        }
    }
}