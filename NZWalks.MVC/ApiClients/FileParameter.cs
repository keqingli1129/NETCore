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
//
// Stream ownership: HttpRequestMessage.Dispose() cascades to the StreamContent
// wrapping Data, which disposes Data in turn, so nothing here needs to dispose
// it itself (and disposing System.IO.Stream.Null, used for the no-image case,
// is a no-op anyway). But that also means Data is single-use: the first send
// consumes and disposes the underlying stream, so if retry/Polly logic were
// ever added, a retried send of the same FileParameter would fail against an
// already-disposed stream. A retry path would need to re-open the source
// (e.g. call IFormFile.OpenReadStream() again) rather than reuse the instance.
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
