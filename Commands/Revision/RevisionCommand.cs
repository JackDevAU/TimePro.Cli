using System.ComponentModel;
using Timepro.Timesheet.Shared;

namespace Timepro.Timesheet.Commands.Revision;

public enum RevisionOptions
{
    Location
}

public enum ChangeOptions
{
    SpecificDate,
    Today,
    Yesterday,
    ThisWeek,
    LastWeek,
    ThisMonth,
    LastMonth,
    ThisYear,
    LastYear
}

public class RevisionSettings : CommandSettings
{
    public ChangeOptions? ChangeOptions { get; set; }

    [Description("The From date to select")]
    public string From { get; set; } = string.Empty;

    [Description("The To date to select")] public string To { get; set; } = string.Empty;
    public required List<RevisionOptions> SelectedRevisionOptions { get; set; }
}

public class RevisionCommand : AsyncCommand<RevisionSettings>
{
    private readonly ApiConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public RevisionCommand(ApiConfig config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RevisionSettings settings)
    {
        settings.ChangeOptions = AnsiConsole.Prompt(
            new SelectionPrompt<ChangeOptions>()
                .Title("Which [green]change option[/] would you like to select?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more content sections)[/]")
                .AddChoices(Enum.GetValues<ChangeOptions>()));

        if (settings.ChangeOptions == ChangeOptions.SpecificDate)
        {
            settings.From =
                AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter the first date you want to change [blue]From Date[/] (dd/mm/yy) "));
            settings.To =
                AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter the last date you want to change [blue]To Date[/] (dd/mm/yy) Default value: ")
                        .DefaultValue(DateOnly.FromDateTime(DateTime.Now).ToString())
                        .ShowDefaultValue());
        }

        // Prompt the user for parser selection
        settings.SelectedRevisionOptions = AnsiConsole.Prompt(
            new MultiSelectionPrompt<RevisionOptions>()
                .Title("Which [green]revision option[/] would you like to select?")
                .Required()
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more content sections)[/]")
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a content section, " +
                    "[green]<enter>[/] to accept)[/]")
                .AddChoices(Enum.GetValues<RevisionOptions>()));

        foreach (var parseContent in settings.SelectedRevisionOptions)
        {
            switch (parseContent)
            {
                case RevisionOptions.Location:
                    await Location.Execute(_httpClientFactory, _config, settings);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return 0;
    }
}