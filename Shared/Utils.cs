using System.Globalization;
using System.Text;
using System.Text.Json;
using Spectre.Console.Json;

namespace Timepro.Timesheet.Shared;

public class LocationStruct
{
    public string? LocationName { get; set; }
    public string? LocationID { get; set; }
}

public class ValidDate
{
    public DateTime date { get; set; }
    public int category { get; set; }
}

public class SelectDayTimeSheet
{
    public int TimeID { get; set; }
    public string? EmpID { get; set; }
    public string? EmpName { get; set; }
    public string? Client { get; set; }
    public string? ClientId { get; set; }
    public string? Project { get; set; }
    public string? ProjectID { get; set; }
    public string? Iteration { get; set; }
    public string? Category { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public DateTime Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? BillableID { get; set; }
    public bool IsBillable { get; set; }
    public decimal Less { get; set; }
    public decimal TotalTime { get; set; }
    public bool HasNotes { get; set; }
    public bool IsSuggested { get; set; }
}

public class DayTimeSheet
{
    public int TimesheetID { get; set; }
    public string? Note { get; set; }
    public decimal TimeTotal { get; set; }
    public decimal TimeBillable { get; set; }
    public bool IsOverridden { get; set; }
    public bool IsOverwriteRate { get; set; }
    public int? InvoiceID { get; set; }
    public string? EmpCreated { get; set; }
    public string? EmpUpdated { get; set; }
    public object? ExternalSync { get; set; }
    public string? EmpID { get; set; }
    public string? EmpName { get; set; }
    public string? ClientID { get; set; }
    public string? ClientName { get; set; }
    public string? ProjectID { get; set; }
    public string? ProjectType { get; set; }
    public int? IterationID { get; set; }
    public string? CategoryID { get; set; }
    public string? CategoryName { get; set; }
    public bool IsNonWorkingCategory { get; set; }
    public string? Location { get; set; }
    public string? LocationID { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int TimeLess { get; set; }
    public decimal SellPrice { get; set; }
    public decimal SalesTaxPct { get; set; }
    public decimal SalesTaxAmt { get; set; }
    public decimal SellTotal { get; set; }
    public string? BillableID { get; set; }
    public bool HasAzureDevOpsSettings { get; set; }
    public decimal PrepaidRate { get; set; }
    public decimal RegularRate { get; set; }
    public string? TimesheetStartTime { get; set; }
    public string? TimesheetEndTime { get; set; }
}

public class SaveTimeSheet
{
    public string? TimeID { get; set; }
    public string? EmpID { get; set; }
    public string? ClientID { get; set; }
    public string? CategoryID { get; set; }
    public DateTime DateCreated { get; set; }
    public string? LocationID { get; set; }
    public bool IsBillingTypeOverridden { get; set; }
    public bool IsOverwriteRate { get; set; }
    public string? ProjectID { get; set; }
    public string? SalesTaxAmt { get; set; }
    public double SalesTaxPct { get; set; }
    public string? SellPrice { get; set; }
    public string? SellTotal { get; set; }
    public string? TimeEnd { get; set; }
    public string? TimeStart { get; set; }
    public int TimeLess { get; set; }
    public string? BillableID { get; set; }
    public int TimeTotal { get; set; }
    public int? IterationId { get; set; } 
    public string? Notes { get; set; }
    public int? InvoiceID { get; set; } 
}

public static class Utils
{
    public static SaveTimeSheet ConvertToSaveTimeSheet(this DayTimeSheet? dayTimeSheet)
    {
        return new SaveTimeSheet
        {
            TimeID = dayTimeSheet.TimesheetID.ToString(),
            EmpID = dayTimeSheet.EmpID,
            ClientID = dayTimeSheet.ClientID,
            CategoryID = dayTimeSheet.CategoryID,
            DateCreated = dayTimeSheet.DateCreated,
            LocationID = dayTimeSheet.LocationID,
            IsBillingTypeOverridden = dayTimeSheet.IsOverridden,
            IsOverwriteRate = dayTimeSheet.IsOverwriteRate,
            ProjectID = dayTimeSheet.ProjectID,
            SalesTaxAmt = dayTimeSheet.SalesTaxAmt.ToString(CultureInfo.InvariantCulture),
            SalesTaxPct = (double)dayTimeSheet.SalesTaxPct,
            SellPrice = dayTimeSheet.SellPrice.ToString(CultureInfo.InvariantCulture),
            SellTotal = dayTimeSheet.SellTotal.ToString(CultureInfo.InvariantCulture),
            TimeEnd = dayTimeSheet.EndTime,
            TimeStart = dayTimeSheet.StartTime,
            TimeLess = dayTimeSheet.TimeLess / 60, // NOTE: Need to divide by 60 as its stored in hours here
            BillableID = dayTimeSheet.BillableID,
            TimeTotal = (int)dayTimeSheet.TimeTotal, 
            IterationId = dayTimeSheet.IterationID,
            Notes = dayTimeSheet.Note,
            InvoiceID = dayTimeSheet.InvoiceID
        };
    }

    /// <summary>
    /// Gets a list of all available locations from the TimePro API
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    public static async Task<List<LocationStruct>?> GetTimeSheetLocation(this HttpClient client)
    {
        //https://ssw.sswtimepro.com/api/Timesheets/GetTimesheetLocation
        var results = await client.GetAsync("Timesheets/GetTimesheetLocation");
        results.EnsureSuccessStatusCode();

        var response = await results.Content.ReadAsStringAsync();
        var locations = JsonSerializer.Deserialize<List<LocationStruct>>(response);

        // TODO: Add logging sometime...
        // AnsiConsole.Write(
        //     new Panel(
        //             new JsonText($"{response}"))
        //         .Header("TimePro Locations")
        //         .Collapse()
        //         .RoundedBorder()
        //         .BorderColor(Color.Yellow));

        return locations;
    }

    /// <summary>
    /// Returns an array of valid dates that have time sheets for the given month and year
    /// </summary>
    /// <param name="client"></param>
    /// <param name="employeeId"></param>
    /// <param name="date">THIS DATE NEEDS TO BE FORMATTED USING -> FormatDateToIso8601</param>
    public static async Task<List<ValidDate>?> GetCalenderInfo(this HttpClient client, string employeeId, string date)
    {
        //https://ssw.sswtimepro.com/api/Timesheets/GetCalenderEvent?empID=<>&date=2023-08-23T14:00:00.000Z
        // empID=<>
        // date=2023-08-23T14:00:00.000Z
        var results = await client.GetAsync($"Timesheets/GetCalenderEvent?empID={employeeId}&date={date}");
        results.EnsureSuccessStatusCode();

        var response = await results.Content.ReadAsStringAsync();
        var dates = JsonSerializer.Deserialize<List<ValidDate>>(response);
        return dates;
    }

    /// <summary>
    /// Get the time sheet info for a given time sheet id
    /// </summary>
    /// <param name="client"></param>
    /// <param name="timeId"></param>
    public static async Task<DayTimeSheet?> GetTimeSheetInfo(this HttpClient client, string timeId)
    {
        //https://ssw.sswtimepro.com/api/Timesheets/GetEditTimesheetsView?timeID=<>
        // timeID: <>
        var results = await client.GetAsync($"Timesheets/GetEditTimesheetsView?timeID={timeId}");
        results.EnsureSuccessStatusCode();

        var response = await results.Content.ReadAsStringAsync();
        var timeSheet = JsonSerializer.Deserialize<DayTimeSheet>(response);

        // TODO: Add logging sometime...
        AnsiConsole.Write(
            new Panel(
                    new JsonText($"{response}"))
                .Header("Timesheet")
                .Collapse()
                .RoundedBorder()
                .BorderColor(Color.Yellow));

        return timeSheet;
    }

    /// <summary>
    /// Returns a list of SelectDayTimeSheet objects for a given day and employee
    /// </summary>
    /// <param name="client"></param>
    /// <param name="employeeId"></param>
    /// <param name="date"></param>
    public static async Task<List<SelectDayTimeSheet>> GetSelectedDayTimeSheetIds(this HttpClient client,
        string employeeId, string date)
    {
        // https://ssw.sswtimepro.com/api/Timesheets/GetTimesheetListViewModel?employeeID=<>&date=2023-08-31T14:00:00.000Z&updatedRecord=null
        // employeeID: <>
        // date: 2023-08-31T14:00:00.000Z
        // updatedRecord: null

        if (string.IsNullOrEmpty(date))
            throw new ArgumentNullException(nameof(date), message: "Date cannot be null or empty");

        var results =
            await client.GetAsync(
                $"Timesheets/GetTimesheetListViewModel?employeeID={employeeId}&date={date}&updatedRecord=null");
        results.EnsureSuccessStatusCode();

        var response = await results.Content.ReadAsStringAsync();
        var timeSheets = (JsonSerializer.Deserialize<List<SelectDayTimeSheet>>(response) ??
                          new List<SelectDayTimeSheet>())
            .Where(timesheet => !timesheet.IsSuggested)
            .ToList();

        return timeSheets;
    }

    public static DateTime GetClosestDate(IEnumerable<DateTime> dates, DateTime from, DateTime to)
    {
        var utcFrom = from.ToUniversalTime();
        var utcTo = to.ToUniversalTime();
        return dates
            .Where(date => date >= utcFrom && date <= utcTo) // Filter dates within the range
            .OrderBy(date => Math.Abs((date - utcFrom).Ticks)) // Order by the absolute difference from the "from" date
            .FirstOrDefault(); // Take the closest date
    }

    /// <summary>
    /// Formats a DateTime to the correct format that the TimePro API expects
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string FormatDateToIso8601(this DateTime date)
    {
        return date.ToUniversalTime().ToString("o");
    }

    public static bool IsMondayOrTuesday(this DateTime date)
    {
        return date.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Tuesday;
    }

    /// <summary>
    /// Makes a POST Request to the TimePro API to save a time sheet
    /// </summary>
    /// <param name="client"></param>
    /// <param name="timeSheet"></param>
    /// <returns></returns>
    public static async Task<string?> SaveTimeSheet(this HttpClient client, SaveTimeSheet timeSheet)
    {
        //https://ssw.sswtimepro.com/api/Timesheets/SaveTimesheet?isEdit=true&isSuggested=false
        // isEdit: true
        // isSuggested: false
        // Body: SaveTimeSheet

        try
        {
            var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(timeSheet);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response =
                await client.PostAsync($"Timesheets/SaveTimesheet?isEdit=true&isSuggested=false", httpContent);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            // Handle any other unforeseen errors
            AnsiConsole.MarkupLine($"[red]Failed to save timesheet.[/] {ex?.InnerException?.Message ?? "Likely due to Invoice lock"}.");
            return null;
        }
    }
}