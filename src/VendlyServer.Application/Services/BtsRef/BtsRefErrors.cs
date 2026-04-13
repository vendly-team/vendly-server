namespace VendlyServer.Application.Services.BtsRef;

public static class BtsRefErrors
{
    public static readonly Error RegionNotFound         = Error.NotFound("BtsRef.RegionNotFound");
    public static readonly Error RegionCodeExists       = Error.Conflict("BtsRef.RegionCodeAlreadyExists");
    public static readonly Error CityNotFound           = Error.NotFound("BtsRef.CityNotFound");
    public static readonly Error CityCodeExists         = Error.Conflict("BtsRef.CityCodeAlreadyExists");
    public static readonly Error BranchNotFound         = Error.NotFound("BtsRef.BranchNotFound");
    public static readonly Error BranchCodeExists       = Error.Conflict("BtsRef.BranchCodeAlreadyExists");
    public static readonly Error PackageTypeNotFound    = Error.NotFound("BtsRef.PackageTypeNotFound");
    public static readonly Error PackageTypeBtsIdExists = Error.Conflict("BtsRef.PackageTypeBtsIdAlreadyExists");
    public static readonly Error PostTypeNotFound       = Error.NotFound("BtsRef.PostTypeNotFound");
    public static readonly Error PostTypeBtsIdExists    = Error.Conflict("BtsRef.PostTypeBtsIdAlreadyExists");
}
