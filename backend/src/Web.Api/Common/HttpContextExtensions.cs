namespace Web.Api.Common;

public static class HttpContextExtensions
{
    public static Uri ToUriFullAbsolutePath(this HttpContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        return new($"{ctx.Request.Scheme}://{ctx.Request.Host.Value}{ctx.Request.Path}",
            UriKind.Absolute);
    }
}