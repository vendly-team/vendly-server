namespace VendlyServer.Application.Services.Storage;

public static class StorageErrors
{
    public static readonly Error InvalidExtension = Error.Failure("Storage.InvalidExtension");
    public static readonly Error FileTooLarge = Error.Failure("Storage.FileTooLarge");
    public static readonly Error UploadFailed = Error.Failure("Storage.UploadFailed");
    public static readonly Error DeleteFailed = Error.Failure("Storage.DeleteFailed");
    public static readonly Error InvalidUrl = Error.Failure("Storage.InvalidUrl");
}
