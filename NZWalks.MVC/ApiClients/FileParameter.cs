namespace NZWalks.MVC.ApiClients;

// NSwag normally emits this helper class itself, but it fails to do so for this spec:
// NZWalks.API's Regions POST/PUT body types the Image property as
// { "$ref": "#/components/schemas/IFormFile" } rather than an inline binary schema.
// That $ref indirection is a known NSwag limitation (see RicoSuter/NSwag issues #4392
// and #4507 - "$ref" multipart/form-data file schemas produce inconsistent client
// output). NSwag still correctly types the generated Regions*Async parameters as
// FileParameter (see nzwalks.v1Client.cs), it just never emits the class definition,
// so the generated file fails to compile without this hand-authored replacement.
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
