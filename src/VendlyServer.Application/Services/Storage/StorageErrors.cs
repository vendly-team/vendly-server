namespace VendlyServer.Application.Services.Storage;

public static class StorageErrors
{
    public static readonly Error InvalidExtension = Error.Failure("Storage.InvalidExtension");
    public static readonly Error FileTooLarge = Error.Failure("Storage.FileTooLarge");
}
