namespace InventoryService.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class InsufficientStockException : Exception
{
    public InsufficientStockException(string message) : base(message) { }
}
