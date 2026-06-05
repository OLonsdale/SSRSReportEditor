using System.Xml.Linq;

namespace ReportEditor.Models;

public sealed class EmbeddedImage : RdlElement
{
    public EmbeddedImage(XElement xml) : base(xml) { }

    public string Name { get => GetAttr("Name") ?? ""; set => SetAttr("Name", value); }
    public string MimeType
    {
        get => GetEl("MIMEType") ?? "image/png";
        set => SetEl("MIMEType", value);
    }
    public string ImageData
    {
        get => GetEl("ImageData") ?? "";
        set => SetEl("ImageData", value);
    }

    public string DataUrl => $"data:{MimeType};base64,{ImageData}";
}
