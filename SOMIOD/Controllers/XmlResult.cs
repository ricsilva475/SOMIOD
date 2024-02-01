// XmlResult.cs
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Net.Http.Headers;

public class XmlResult<T> : IHttpActionResult
{
    private readonly T _content;

    public XmlResult(T content)
    {
        _content = content;
    }

    public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ObjectContent<T>(_content, new System.Net.Http.Formatting.XmlMediaTypeFormatter())
        };

        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

        return Task.FromResult(response);
    }
}
