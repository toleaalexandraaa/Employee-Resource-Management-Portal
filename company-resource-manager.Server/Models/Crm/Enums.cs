namespace company_resource_manager.Server.Models.Crm;

public enum ClientStatus
{
    Active,
    Inactive,
    Prospect,
}

public enum ContractStatus
{
    Draft,
    Sent,
    Signed,
    Expired,
    Terminated,
}

public enum InvoiceStatus
{
    Draft,
    Sent,
    PartiallyPaid,
    Paid,
    Overdue,
    Cancelled,
}

public enum LeadSource
{
    Website,
    Referral,
    LinkedIn,
    ColdOutreach,
    Event,
    Other,
}

public enum LeadStage
{
    New,
    Contacted,
    Qualified,
    ProposalSent,
    Won,
    Lost,
}
