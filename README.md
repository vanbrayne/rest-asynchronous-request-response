# rest-asynchronous-request-response
Proof of concept for a .NET Core middleware that can make any API method support asynchronous execution.

## Background

When a client calls an API method, it is normally executed asynchronously, i.e. the HTTP connection is open until the response is returned.

There is a proposed standard for a HTTP header, [rfc7240](https://www.rfc-editor.org/info/rfc7240), where a calling client can state "Prefer: respond-async".
> The "respond-async" preference indicates that the client prefers the server to respond asynchronously to a response.

## Objective

To enable all methods of a REST API to honor the `respond-async` preference.

## Solution

I have created an ASP.NET Core middleware that detects the `respond-async` preference and then enqueues the request for later execution and returns a `202 Accepted` response to the calling client. That response contains the URL where the client can poll for the final response. A background thread executes the requests one-by-one and stores the response in a data dictionary.

## How to use

Build and run.

In the browser:

1. GET /tests/hello, should return "Hello world!"
2. GET /tests/slow, returns after about 10 seconds with a message how long time it took.
3. GET /tests/slow, but now with HTTP header `Prefer: respond-async`. This should return a link where the response can be found. Note: Don't set header `Accept-Encoding`, or the final response in the next step might be zipped.
4. GET on the URL in the response. If you do this before the request has been completed, then you will get the same response with the link again. When the request has completed, you will get the final response, i.e. an object with the different parts of the response, including a message of how long time it took.

## Future improvements

### Don't call the application server

The middleware method `MaybeRunAsynchronously.GetHttpClientForInternalCalls()` is where we create an `HttpClient` to use when we want to execute a request on the background thread. Currently it creates uses `HttpClientFactory.Create()`, but that means that all calls are routed through the application server (typically IIS). This should be improved for a number of reasons where the main reason is that the applicaiton server will timeout if the execution of a request takes too long time (default for IIS is 110 seconds).

In the code I have shown with an example what I want to achieve; I try to use `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<T>`. It is just there to illustrate what I want to do. I don´'t want to rely on a NuGet library aimed at testing and besides that I could make that code work as it conflicts with IIS:
> System.InvalidOperationException: 'Application is running inside IIS process but is not configured to use IIS server.'

### Location URL is a hack

Currently, the first call to the service has to be to the root, because the `RequestsController.Initialize()` method must be called before any asynchronous calls are made.






