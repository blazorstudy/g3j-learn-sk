using System;
using System.ComponentModel;
using System.Linq;

using Microsoft.SemanticKernel;

public class BookingsPlugin
{
    private readonly string _customerTimeZone = "Asia/Seoul";
    private static readonly Random random = new Random();

    [KernelFunction("ListRestaurant")]
    [Description("Lists the available restaurants for booking.")]
    public async Task<List<string>> ListRestaurantAsync(
        [Description("The time in UTC")] DateTimeOffset dateTime,
        [Description("Number of people in your party")] int partySize
    )
    {
        Console.WriteLine($"System > {dateTime}에 {partySize}명 예약 가능한 식당 목록입니다.");

        return new List<string> { "맛있는 식당", "푸짐한 식당", "분위기 좋은 식당" };
    }

    [KernelFunction("BookTable")]
    [Description("Books a new table at a restaurant")]
    public async Task<string> BookTableAsync(
        [Description("Name of the restaurant")] string restaurant,
        [Description("The time in UTC")] DateTimeOffset dateTime,
        [Description("Number of people in your party")] int partySize,
        [Description("Customer name")] string customerName,
        [Description("Customer email")] string customerEmail,
        [Description("Customer phone number")] string customerPhone
    )
    {
        Console.WriteLine($"System > {restaurant} 식당에 {customerName} 이름으로 {dateTime}에 {partySize}명 예약하시겠습니까?");
        Console.WriteLine("System > '예' 또는 '아니오'로 답해주세요.");
        Console.Write("User > ");
        var response = await Microsoft.DotNet.Interactive.Kernel.GetInputAsync("예약 확인");
        Console.WriteLine(response);
        if (string.Equals(response, "예", StringComparison.InvariantCultureIgnoreCase))
        {
            return "예약을 마쳤습니다";
        }

        return "예약을 멈췄습니다";
    }

    [KernelFunction]
    [Description("List reservations booking at a restaurant.")]
    public async Task<List<Appointment>> ListReservationsAsync()
    {
        var seoulZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
        var seoulToday = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, seoulZone).Date;
        var startTime = seoulToday.AddHours(11);

        var appointments = Enumerable.Range(0, 5)
                                     .Select(i => new Appointment 
                                     { 
                                         Start = new DateTimeOffset(startTime.AddHours(i * 2), seoulZone.BaseUtcOffset),
                                         Restaurant = "맛있는 식당",
                                         PartySize = random.Next(1, 8),
                                         ReservationId = Guid.NewGuid().ToString()
                                     })
                                     .ToList();

        return await Task.FromResult(appointments);
    }

    [KernelFunction]
    [Description("Cancels a reservation at a restaurant.")]
    public async Task<string> CancelReservationAsync(
        [Description("The appointment ID to cancel")] string appointmentId,
        [Description("Name of the restaurant")] string restaurant,
        [Description("The date of the reservation")] string date,
        [Description("The time of the reservation")] string time,
        [Description("Number of people in your party")] int partySize)
    {
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($"System > [Cancelling a reservation for {partySize} at {restaurant} on {date} at {time}]");
        Console.ResetColor();

        return "예약을 취소했습니다";
    }
}
