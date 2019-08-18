# SideCar

SideCar is a small piece of middleware plus a "web server" extension for ASP.NET Core that simplifies executing C# code in browsers via WASM.

The middleware handles fetching Mono WASM SDKs, compiling packages, and serving files that virtually map from a typical browser request URL to
the relevant build.

With the middleware installed, making a C# assembly available for execution in WASM is as simple as referencing it in a query string.

```html
<head>
    <script defer type="text/javascript" src="mono-config.js?p=MyAssemblyName"></script>
    <script defer type="text/javascript" src="runtime.js?p=MyAssemblyName"></script>
    <script defer type="text/javascript" src="mono.js"></script>
</head>
```