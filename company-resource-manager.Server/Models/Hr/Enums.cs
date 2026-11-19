namespace company_resource_manager.Server.Models.Hr;

public enum ContractType
{
    FullTime,
    PartTime,
    Freelance,
    Internship,
}

public enum DocumentType
{
    CV,
    IdCard,
    Contract,
    Other,
}

public enum LeaveType
{
    Annual,
    Medical,
    Unpaid,
    Parental,
    Other,
}

public enum LeaveStatus
{
    Pending,
    Approved,
    Rejected,
}

public enum PayrollStatus
{
    Draft,
    Approved,
    Paid,
}
