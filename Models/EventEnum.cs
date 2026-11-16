namespace SSKT_Events.Models;

public enum EventStatus
{
    Draft,
    Upcoming,
    Ongoing,
    Completed,
    Cancelled,
    Pending
}

public enum EventType
{
    Birthday,
    Wedding,
    Debut,
    Other
}

public enum BookingStatus
{
    Pending,
    Accepted,
    Rejected,
    Cancelled
}