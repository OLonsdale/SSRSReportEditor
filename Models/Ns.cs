using System.Xml.Linq;

namespace ReportEditor.Models;

public static class Ns
{
    public static readonly XNamespace R =
        "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition";

    public static readonly XNamespace Rd =
        "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner";
}
