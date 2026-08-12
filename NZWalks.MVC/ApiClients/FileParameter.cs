namespace NZWalks.MVC.ApiClients;

// NSwag normally emits this helper class itself, but it doesn't for this spec.
// Directly observed in NZWalks.MVC/obj/nzwalks.v1Client.cs: the generated
// Regions*Async methods (POST/PUT) type their image parameter as FileParameter
// - e.g. `RegionsPOSTAsync(string code, string name, FileParameter image, ...)` -
// and reference FileParameter.Data/.FileName/.ContentType in the method bodies,
// but no `class FileParameter` definition appears anywhere in that file. Without
// this hand-authored replacement, the generated file fails to compile with
// CS0246 ("The type or namespace name 'FileParameter' could not be found").
// The likely trigger is that NZWalks.API's Regions POST/PUT body types the Image
// property as { "$ref": "#/components/schemas/IFormFile" } rather than an inline
// binary schema, but that is inference, not a confirmed NSwag defect/issue number.
// Shape matches NSwag's standard FileParameter template exactly so it is a drop-in
// stand-in if a future NSwag/spec change makes generation emit it after all
// (in which case this file should be deleted to avoid a duplicate-type error).
public partial class FileParameter
{
    public FileParameter(System.IO.Stream data)
        : this(data, null)
    {
    }

    public FileParameter(System.IO.Stream data, string? fileName)
        : this(data, fileName, null)
    {
    }

    public FileParameter(System.IO.Stream data, string? fileName, string? contentType)
    {
        Data = data;
        FileName = fileName;
        ContentType = contentType;
    }

    public System.IO.Stream Data { get; private set; }

    public string? FileName { get; private set; }

    public string? ContentType { get; private set; }
}
