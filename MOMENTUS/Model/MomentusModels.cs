using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOMENTUS.Model
{
    public class MomentusEvent
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? EventTypeId { get; set; }
        public string? EventTypeName { get; set; }
        public string? AccountId { get; set; }
        public string? AccountName { get; set; }
        public bool IsInternal { get; set; }
        public bool IsBlackout { get; set; }
        public DateOnly? Start { get; set; }
        public DateOnly? End { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public ICollection<string>? VenueIds { get; set; }
        public ICollection<string>? VenueNames { get; set; }
        public ICollection<string>? RoomIds { get; set; }
        public ICollection<string>? RoomNames { get; set; }
        public string? Description { get; set; }
        public string? BusinessClassificationId { get; set; }
        public string? BusinessClassificationName { get; set; }
        public string? GenreId { get; set; }
        public string? GenreName { get; set; }
        public string? UniqueNumber { get; set; }
        public string? Website { get; set; }
        public string? MarketSegment { get; set; }
        public string? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? LastModifiedOn { get; set; }
        public string? AccountUniqueNumber { get; set; }
        public bool IsAccountTaxExempt { get; set; }
        public DateOnly? DecisionDate { get; set; }
        public int? ProbabilityOfClosing { get; set; }
        public int? EstimatedTotalAttendance { get; set; }
        public DateTime? BookedDefiniteOn { get; set; }
        public DateTime? BookedOn { get; set; }
        public decimal? ExpectedRevenue { get; set; }
        public decimal? TotalActualRevenue { get; set; }
        public int? EstimatedAttendance { get; set; }
        public int? ActualAttendance { get; set; }
        public bool IsTentative { get; set; }
        public bool IsDefinite { get; set; }
        public bool IsProspect { get; set; }
        public bool IsCanceled { get; set; }
        public bool IsActive { get; set; }
        public bool IsEventHiddenFromNonBookers { get; set; }
        public bool IsFinalized { get; set; }
        public bool IsLost { get; set; }
        public string? ReasonLost { get; set; }
        public string? ApprovedProposalId { get; set; }
        public string? ActiveContractId { get; set; }
        public bool HasContract { get; set; }
        public bool IsTentativeNoContract { get; set; }
        public bool IsDefiniteNoContract { get; set; }
        public bool IsPendingContract { get; set; }
        public bool HasNoProposalOrContract { get; set; }
        public bool IsProposalCreated { get; set; }
        public bool IsProposalSent { get; set; }
        public bool IsProposalApproved { get; set; }
        public bool IsContractWaitingToSend { get; set; }
        public bool IsContractWaitingToSendForApproval { get; set; }
        public bool IsContractPendingApproval { get; set; }
        public bool IsContractSent { get; set; }
        public bool IsContractSigned { get; set; }
        public bool ShowNameOnAvails { get; set; }
        public string? SeriesId { get; set; }
        public string? SeriesName { get; set; }
        public ICollection<MomentusExternalId>? ExternalIds { get; set; }
        public ICollection<MomentusContactRole>? ContactRoles { get; set; }
        public ICollection<MomentusTag>? Tags { get; set; }
        public ICollection<MomentusStaffAssignment>? StaffAssignments { get; set; }
        public ICollection<string>? StaffIds { get; set; }
        public MomentusLiveEntertainment? LiveEntertainment { get; set; }
        public ICollection<MomentusBookedSpace>? BookedSpaces { get; set; }
    }

    public class MomentusExternalId
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
        public string? DisplayName { get; set; }
    }

    public class MomentusContactRole
    {
        public string? ContactId { get; set; }
        public string? AccountId { get; set; }
        public string? AccountName { get; set; }
        public string? Role { get; set; }
        public string? Title { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? EmailType { get; set; }
        public string? Phone { get; set; }
        public string? PhoneType { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressStateProvince { get; set; }
        public string? AddressPostalCode { get; set; }
        public string? AddressCountry { get; set; }
        public string? AddressType { get; set; }
    }

    public class MomentusTag
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
    }

    public class MomentusStaffAssignment
    {
        public string? StaffMemberId { get; set; }
        public string? StaffMemberName { get; set; }
        public string? StaffMemberEmail { get; set; }
        public string? StaffAssignmentId { get; set; }
        public string? StaffAssignmentName { get; set; }
    }

    public class MomentusLiveEntertainment
    {
        public string? EventId { get; set; }
        public string? EventName { get; set; }
        public string? Note { get; set; }
        public string? ArtistId { get; set; }
        public string? ArtistName { get; set; }
        public DateOnly? AnnounceDate { get; set; }
        public string? AnnounceTime { get; set; }
        public DateOnly? OnSaleDate { get; set; }
        public string? OnSaleTime { get; set; }
        public ICollection<MomentusPreSaleDate>? PreSaleDates { get; set; }
        public ICollection<MomentusPerformance>? Performances { get; set; }
    }

    public class MomentusPreSaleDate
    {
        public string? Name { get; set; }
        public DateOnly? Date { get; set; }
        public string? Time { get; set; }
    }

    public class MomentusPerformance
    {
        public string? FunctionId { get; set; }
    }

    public class MomentusBookedSpace
    {
        public string? BookedSpaceId { get; set; }
        public string? RoomId { get; set; }
        public string? RoomName { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool IsAllDay { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? BookedStatus { get; set; }
        public string? UsageType { get; set; }
    }

    public class MomentusRoom
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int? MaxCapacity { get; set; }
        public int? SquareFootage { get; set; }
        public string? VenueId { get; set; }
        public string? VenueName { get; set; }
        public string? Group { get; set; }
        public string? ItemCode { get; set; }
        public ICollection<string>? SubRoomIds { get; set; }
        public bool IsComboRoom { get; set; }
        public bool IsActive { get; set; }
        public ICollection<string>? ConflictingRoomIds { get; set; }
    }

    public class MomentusFunction
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? EventId { get; set; }
        public string? EventName { get; set; }
        public string? VenueName { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool IsAllDay { get; set; }
        public string? FunctionStatus { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? FunctionTypeId { get; set; }
        public string? FunctionTypeName { get; set; }
        public string? RoomId { get; set; }
        public string? RoomName { get; set; }
        public bool IsEventHidden { get; set; }
        public string? RoomSetup { get; set; }
        public int? AgreedAttendance { get; set; }
        public int? ExpectedAttendance { get; set; }
        public int? GuaranteedAttendance { get; set; }
        public bool IsEventWide { get; set; }
        public bool ShowOnCalendar { get; set; }
        public bool IsPerformance { get; set; }
        public bool IsInvoiced { get; set; }
        public ICollection<MomentusExternalId>? ExternalIds { get; set; }
        public ICollection<MomentusStaffAssignment>? StaffAssignments { get; set; }
        public ICollection<string>? StaffIds { get; set; }
    }



    public class MomentusTokenResponse
    {
        public string? AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? TokenType { get; set; }
    }

    public class EventSearchRequest
    {
        public DateOnly? Start { get; set; }
        public DateOnly? End { get; set; }
        public ICollection<string>? VenueIds { get; set; }
        public ICollection<string>? RoomIds { get; set; }
        public bool IncludeBookedSpaces { get; set; }
    }
}
