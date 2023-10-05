using Timepro.Timesheet.Shared;

namespace Timepro.Timesheet.Commands.Revision;

public static class Location
{
    public static async Task Execute(IHttpClientFactory httpClientFactory, ApiConfig _config, RevisionSettings settings)
    {
        var httpClient = httpClientFactory.CreateClient("api");
        var (startDate, endDate) = CalculateTargetDate(settings.ChangeOptions, settings);
        var dateRange = GetDateRange(startDate, endDate).ToList();

        if (dateRange.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No valid timesheets found for selected date range.[/]");
            return;
        }

        foreach (var date in dateRange)
        {
            var timesheetValidDates = await httpClient.GetCalenderInfo(_config.EmpId, date.FormatDateToIso8601());
            if (timesheetValidDates.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No valid timesheet dates found for " + date.ToShortDateString() + ".[/]");
                continue;
            }

            var validTimesheets =
                await httpClient.GetSelectedDayTimeSheetIds(_config.EmpId, date.FormatDateToIso8601());
            var mondayTuesdayTimesheets =
                validTimesheets.Where(timesheet => timesheet.Date.IsMondayOrTuesday()).ToList();

            var validTimesheetCount = await ProcessTimeSheets(httpClient, _config, mondayTuesdayTimesheets);

            AnsiConsole.MarkupLine(
                $"[yellow]Valid Timesheets for {date.ToShortDateString()}: {validTimesheetCount}[/]");
        }
    }

    private static (DateTime startDate, DateTime endDate) CalculateTargetDate(ChangeOptions? changeOption,
        RevisionSettings settings)
    {
        var today = DateTime.Now.Date;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var startOfYear = new DateTime(today.Year, 1, 1);

        return changeOption switch
        {
            ChangeOptions.SpecificDate => (DateTime.Parse(settings.From), DateTime.Parse(settings.To)),
            ChangeOptions.Today => (today, today),
            ChangeOptions.Yesterday => (today.AddDays(-1), today.AddDays(-1)),
            ChangeOptions.ThisWeek => (startOfWeek, today),
            ChangeOptions.LastWeek => (startOfWeek.AddDays(-7), startOfWeek),
            ChangeOptions.ThisMonth => (startOfMonth, today),
            ChangeOptions.LastMonth => (startOfMonth.AddMonths(-1), startOfMonth),
            ChangeOptions.ThisYear => (startOfYear, today),
            ChangeOptions.LastYear => (startOfYear.AddYears(-1), startOfYear),
            _ => throw new ArgumentOutOfRangeException(nameof(changeOption), "Invalid change option provided.")
        };
    }

    private static async Task<int> ProcessTimeSheets(HttpClient httpClient, ApiConfig config,
        List<SelectDayTimeSheet>? mondayTuesdayTimesheets)
    {
        var validTimesheetCount = 0;

        if (mondayTuesdayTimesheets == null) return validTimesheetCount;

        foreach (var timeSheet in mondayTuesdayTimesheets)
        {
            var getTimeSheetsForCurrentDay =
                await httpClient.GetSelectedDayTimeSheetIds(config.EmpId, timeSheet.Date.FormatDateToIso8601());

            foreach (var dayTimeSheet in getTimeSheetsForCurrentDay)
            {
                var timesheetInfo = await httpClient.GetTimeSheetInfo(dayTimeSheet.TimeID.ToString());
                // TODO: Update Location here
                // TODO: Update this so its dynamic?
                timesheetInfo.LocationID = "Home";
                if (string.IsNullOrEmpty(timesheetInfo.Note))
                    AnsiConsole.MarkupLine(
                        $"[red]Timesheet notes missing [/] for {timeSheet.Date.ToShortDateString()}");

                var updateTimeSheet = await httpClient.SaveTimeSheet(timesheetInfo.ConvertToSaveTimeSheet());
                if (updateTimeSheet is null)
                {
                    AnsiConsole.MarkupLine($"[red]Failed to update timesheet: {timeSheet.Date.ToShortDateString()} [/]");
                    continue;
                }

                AnsiConsole.MarkupLine($"[green]Updated timesheet: {updateTimeSheet}[/]");
                validTimesheetCount++;
            }
        }

        return validTimesheetCount;
    }

    private static IEnumerable<DateTime> GetDateRange(DateTime startDate, DateTime endDate)
    {
        while (startDate <= endDate)
        {
            yield return startDate;
            startDate = startDate.AddDays(1);
        }
    }
}