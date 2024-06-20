﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;
using UIKit;
using WebKit;
using RectangleF = CoreGraphics.CGRect;

namespace Microsoft.Maui.Handlers
{
    public partial class HybridWebViewHandler : ViewHandler<IHybridWebView, WKWebView>
	{
		//protected virtual float MinimumSize => 44f;

		//WKUIDelegate? _delegate;

		protected override WKWebView CreatePlatformView()
		{
			var config = new WKWebViewConfiguration();

			// By default, setting inline media playback to allowed, including autoplay
			// and picture in picture, since these things MUST be set during the webview
			// creation, and have no effect if set afterwards.
			// A custom handler factory delegate could be set to disable these defaults
			// but if we do not set them here, they cannot be changed once the
			// handler's platform view is created, so erring on the side of wanting this
			// capability by default.
			if (OperatingSystem.IsMacCatalystVersionAtLeast(10) || OperatingSystem.IsIOSVersionAtLeast(10))
			{
				config.AllowsPictureInPictureMediaPlayback = true;
				config.AllowsInlineMediaPlayback = true;
				config.MediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypes.None;
			}

            config.UserContentController.AddScriptMessageHandler(new WebViewScriptMessageHandler(MessageReceived), "webwindowinterop");
			// iOS WKWebView doesn't allow handling 'http'/'https' schemes, so we use the fake 'app' scheme
            config.SetUrlSchemeHandler(new SchemeHandler(this), urlScheme: "app");

			var webview = new WKWebView(RectangleF.Empty, config)
			{
				BackgroundColor = UIColor.Clear,
				AutosizesSubviews = true
			};

			if (true)//DeveloperTools.Enabled)
			{
				// Legacy Developer Extras setting.
				config.Preferences.SetValueForKey(NSObject.FromObject(true), new NSString("developerExtrasEnabled"));

				if (OperatingSystem.IsIOSVersionAtLeast(16, 4) || OperatingSystem.IsMacCatalystVersionAtLeast(16, 6))
				{
					// Enable Developer Extras for iOS builds for 16.4+ and Mac Catalyst builds for 16.6 (macOS 13.5)+
					webview.SetValueForKey(NSObject.FromObject(true), new NSString("inspectable"));
				}
			}

			return new HybridPlatformWebView(this, RectangleF.Empty, config);
		}

		public static void MapSendRawMessage(IHybridWebViewHandler handler, IHybridWebView hybridWebView, object? arg) {
			if (arg is not string rawMessage || handler.PlatformView is not IHybridPlatformWebView hybridPlatformWebView)
			{
				return;
			}

			hybridPlatformWebView.SendRawMessage(rawMessage);
		}

        private void MessageReceived(Uri uri, string message)
        {
			VirtualView?.RawMessageReceived(message);
       	}

		protected override void ConnectHandler(WKWebView platformView)
		{
#if DEBUG
			// var logger = MauiContext!.Services!.GetService<ILogger<HybridWebViewHandler>>() ?? NullLogger<HybridWebViewHandler>.Instance;
			// logger.LogInformation("HybridWebViewHandler: Connecting WebView2");
#endif

			base.ConnectHandler(platformView);

			//platformView.WebMessageReceived += OnWebMessageReceived;

// `			await platformView.EnsureCoreWebView2Async();

// 			platformView.CoreWebView2.Settings.AreDevToolsEnabled = true;//EnableWebDevTools;
// 			platformView.CoreWebView2.Settings.IsWebMessageEnabled = true;
// 			platformView.CoreWebView2.AddWebResourceRequestedFilter($"{AppOrigin}*", CoreWebView2WebResourceContext.All);
// 			platformView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;


// 			platformView.Source = new Uri(new Uri(AppOriginUri, "/").ToString());

            using var nsUrl = new NSUrl(new Uri(AppOriginUri, "/").ToString());
            using var request = new NSUrlRequest(nsUrl);

            platformView.LoadRequest(request);

		}

		protected override void DisconnectHandler(WKWebView platformView)
		{
#if DEBUG
			// var logger = MauiContext!.Services!.GetService<ILogger<HybridWebViewHandler>>() ?? NullLogger<HybridWebViewHandler>.Instance;
			// logger.LogInformation("HybridWebViewHandler: Disconnecting WebView2");
#endif

			// platformView.WebMessageReceived -= OnWebMessageReceived;
			// platformView.Close();

			base.DisconnectHandler(platformView);
		}

		protected class HybridPlatformWebView : WKWebView, IHybridPlatformWebView
		{
			private readonly WeakReference<HybridWebViewHandler> _handler;

			public HybridPlatformWebView(HybridWebViewHandler handler, RectangleF frame, WKWebViewConfiguration configuration)
				: base(frame, configuration)
			{
				ArgumentNullException.ThrowIfNull(handler, nameof(handler));
				_handler = new WeakReference<HybridWebViewHandler>(handler);
			}

			public void SendRawMessage(string rawMessage)
			{
				EvaluateJavaScript(
					new NSString($"window.external.receiveMessage({System.Text.Json.JsonSerializer.Serialize(rawMessage)})"),
					(result, error) =>
					{
						// Handle the result or error here
					});
			}
		}

        private sealed class WebViewScriptMessageHandler : NSObject, IWKScriptMessageHandler
        {
			[UnconditionalSuppressMessage("Memory", "MEM0002", Justification = "TODO: Temporary")]
			private readonly Action<Uri, string> _messageReceivedAction;

            public WebViewScriptMessageHandler(Action<Uri, string> messageReceivedAction)
            {
                _messageReceivedAction = messageReceivedAction ?? throw new ArgumentNullException(nameof(messageReceivedAction));
            }

            public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
            {
                if (message is null)
                {
                    throw new ArgumentNullException(nameof(message));
                }
                _messageReceivedAction(AppOriginUri, ((NSString)message.Body).ToString());
            }
        }

        private class SchemeHandler : NSObject, IWKUrlSchemeHandler
        {
            private readonly HybridWebViewHandler _webViewHandler;

            public SchemeHandler(HybridWebViewHandler webViewHandler)
            {
                _webViewHandler = webViewHandler;
            }

            [Export("webView:startURLSchemeTask:")]
            [SupportedOSPlatform("ios11.0")]
            public async void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
            {
                var url = urlSchemeTask.Request.Url?.AbsoluteString ?? "";

                var responseData = await GetResponseBytes(url);

                if (responseData.StatusCode == 200)
                {
                    using (var dic = new NSMutableDictionary<NSString, NSString>())
                    {
                        dic.Add((NSString)"Content-Length", (NSString)(responseData.ResponseBytes.Length.ToString(CultureInfo.InvariantCulture)));
                        dic.Add((NSString)"Content-Type", (NSString)responseData.ContentType);
                        // Disable local caching. This will prevent user scripts from executing correctly.
                        dic.Add((NSString)"Cache-Control", (NSString)"no-cache, max-age=0, must-revalidate, no-store");
                        if (urlSchemeTask.Request.Url != null)
                        {
                            using var response = new NSHttpUrlResponse(urlSchemeTask.Request.Url, responseData.StatusCode, "HTTP/1.1", dic);
                            urlSchemeTask.DidReceiveResponse(response);
                        }
                    }

                    urlSchemeTask.DidReceiveData(NSData.FromArray(responseData.ResponseBytes));
                    urlSchemeTask.DidFinish();
                }
            }

            private async Task<(byte[] ResponseBytes, string ContentType, int StatusCode)> GetResponseBytes(string? url)
            {
                string contentType;

				await Task.Delay(0);

                var fullUrl = url;
                url = HybridWebViewQueryStringHelper.RemovePossibleQueryString(url);

                if (new Uri(url) is Uri uri && AppOriginUri.IsBaseOf(uri))
                {
                    var relativePath = AppOriginUri.MakeRelativeUri(uri).ToString().Replace('\\', '/');

                    var hwv = _webViewHandler.VirtualView;

                    var bundleRootDir = Path.Combine(NSBundle.MainBundle.ResourcePath, _webViewHandler.HybridRoot!);

                    if (string.IsNullOrEmpty(relativePath))
                    {
                        relativePath = _webViewHandler.DefaultFile!.Replace('\\', '/');
                        contentType = "text/html";
                    }
                    else
                    {
                        var requestExtension = Path.GetExtension(relativePath);
                        contentType = requestExtension switch
                        {
                            ".htm" or ".html" => "text/html",
                            ".js" => "application/javascript",
                            ".css" => "text/css",
                            _ => "text/plain",
                        };
                    }

                    // Stream? contentStream = null;

                    // if (contentStream == null)
                    // {
                    //     contentStream = KnownStaticFileProvider.GetKnownResourceStream(relativePath!);
                    // }

                    // if (contentStream is not null)
                    // {
                    //     using var ms = new MemoryStream();
                    //     contentStream.CopyTo(ms);
                    //     return (ms.ToArray(), contentType, StatusCode: 200);
                    // }

                    var assetPath = Path.Combine(bundleRootDir, relativePath);

                    if (File.Exists(assetPath))
                    {
                        return (File.ReadAllBytes(assetPath), contentType, StatusCode: 200);
                    }
                }

                return (Array.Empty<byte>(), ContentType: string.Empty, StatusCode: 404);
            }

            [Export("webView:stopURLSchemeTask:")]
            public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
            {
            }
        }
	}
}