namespace OrderService.Domain.Constants;

/// <summary>
/// Shared constants for order domain logic.
/// </summary>
public static class OrderConstants
{
    /// <summary>
    /// Maximum length for customer name fields.
    /// </summary>
    public const int MaxCustomerNameLength = 200;

    /// <summary>
    /// Maximum length for email fields.
    /// </summary>
    public const int MaxEmailLength = 256;

    /// <summary>
    /// Maximum length for order number fields.
    /// </summary>
    public const int MaxOrderNumberLength = 50;

    /// <summary>
    /// Prefix used when generating order numbers.
    /// </summary>
    public const string OrderNumberPrefix = "ORD";
}
