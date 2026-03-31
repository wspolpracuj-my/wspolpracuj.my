using System;
using System.Collections.Generic;

namespace wspolpracujmy.Models;

public record ServiceDto
{
    // public int Id { get; init; }
    public string Name { get; init; } = default!;
}

public record UserDto
{
    // public int Id { get; init; }
    public string Mail { get; init; } = default!;
    public bool Verified { get; init; }
}

public record MatchDto
{
    // public int Id { get; init; }
    public string CompanyTin { get; init; } = default!;
    public string MatchedCompanyTin { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; }
}

public record CompanyDto
{
    public string Tin { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string? Website { get; init; }
    public string? ContactEmail { get; init; }
    public string? Location { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ServiceDto? Service { get; init; }
    public ServiceDto? Offer { get; init; }
}
