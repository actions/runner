using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

class LogFormatter : InputFormatter
{
    public LogFormatter() {
        SupportedMediaTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream"));
    }

    protected override bool CanReadType(Type t) {
        return t == typeof(string);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        var content = await new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, false).ReadToEndAsync();
        return InputFormatterResult.Success(content);
    }
}