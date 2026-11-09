public class OrganiserDashboardVm
{
    public List<OrganiserEventSummaryVm> Events { get; set; } = new();
}
public class OrganiserEventSummaryVm
{
    public int EventId
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
    public DateTime EventDate
    {
        get; set;
    }
    public int EnrolmentCount
    {
        get; set;
    }
}
