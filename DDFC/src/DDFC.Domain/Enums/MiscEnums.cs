namespace DDFC.Domain.Enums;

public enum NotificationChannel
{
    SMS,
    Email,
    InApp
}

public enum TicketCategory
{
    DocumentIssue,
    PaymentIssue,
    DesignQuery,
    GeneralEnquiry,
    Complaint
}

public enum TicketStatus
{
    Open,
    InProgress,
    Resolved,
    Closed
}

public enum AuthorType
{
    Staff,
    Customer
}

public enum SurveyLanguage
{
    EN,
    UR
}
