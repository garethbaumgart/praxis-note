using System.Text.Json;

namespace PraxisNote.Web.Mcp;

internal static class McpErrorHelper
{
    public static string Serialize(Exception ex) => ex switch
    {
        InvalidOperationException => JsonSerializer.Serialize(new { error = ex.Message }),
        ArgumentException => JsonSerializer.Serialize(new { error = ex.Message }),
        _ => JsonSerializer.Serialize(new { error = "An unexpected error occurred. Please try again." })
    };
}
