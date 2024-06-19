using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Maui.Controls
{
	/// <summary>
	/// A <see cref="View"/> that presents local HTML content in a web view and allows JavaScript and C# code to interop using messages.
	/// </summary>
	public class HybridWebView : View, IHybridWebView
	{
		/// <summary>Bindable property for <see cref="DefaultFile"/>.</summary>
		public static readonly BindableProperty DefaultFileProperty =
			BindableProperty.Create(nameof(DefaultFile), typeof(string), typeof(HybridWebView), defaultValue: "index.html");
		/// <summary>Bindable property for <see cref="HybridRoot"/>.</summary>
		public static readonly BindableProperty HybridRootProperty =
			BindableProperty.Create(nameof(HybridRoot), typeof(string), typeof(HybridWebView), defaultValue: "HybridRoot");


		/// <summary>
		/// Specifies the file within the <see cref="HybridRoot"/> that should be served as the default file. The
		/// default value is <c>index.html</c>.
		/// </summary>
		public string? DefaultFile
		{
			get { return (string)GetValue(DefaultFileProperty); }
			set { SetValue(DefaultFileProperty, value); }
		}

		///// <summary>
		///// Gets or sets the path for initial navigation after the content is finished loading. The default value is <c>/</c>.
		///// </summary>
		//public string StartPath { get; set; } = "/";

		/// <summary>
		///  The path within the app's "Raw" asset resources that contain the web app's contents. For example, if the
		///  files are located in <c>[ProjectFolder]/Resources/Raw/hybrid_root</c>, then set this property to "hybrid_root".
		///  The default value is <c>HybridRoot</c>, which maps to <c>[ProjectFolder]/Resources/Raw/HybridRoot</c>.
		/// </summary>
		public string? HybridRoot
		{
			get { return (string)GetValue(HybridRootProperty); }
			set { SetValue(HybridRootProperty, value); }
		}

		void IHybridWebView.RawMessageReceived(string rawMessage)
		{
			RawMessageReceived?.Invoke(this, new HybridWebViewRawMessageReceivedEventArgs(rawMessage));
		}

		/// <summary>
		/// Raised when a raw message is received from the web view. Raw messages are strings that have no additional processing.
		/// </summary>
		public event EventHandler<HybridWebViewRawMessageReceivedEventArgs>? RawMessageReceived;

		//public void Navigate(string url)
		//{
		//	NavigateCore(url);
		//}

		//		protected override async void OnHandlerChanged()
		//		{
		//			base.OnHandlerChanged();

		//			await InitializeHybridWebView();

		//			HybridWebViewInitialized?.Invoke(this, new HybridWebViewInitializedEventArgs()
		//			{
		//#if ANDROID || IOS || MACCATALYST || WINDOWS
		//                WebView = PlatformWebView,
		//#endif
		//			});

		//			Navigate(StartPath);
		//		}

		public void SendRawMessage(string rawMessage)
		{
			Handler?.Invoke(nameof(IHybridWebView.SendRawMessage), rawMessage);

			//EvaluateJavaScriptAsync($"window.mauiBlazorWebView.receiveMessage({JsonSerializer.Serialize(message)})");
		}

		//		private partial Task InitializeHybridWebView();

		//		private partial void NavigateCore(string url);


		//#if !ANDROID && !IOS && !MACCATALYST && !WINDOWS
		//		private partial Task InitializeHybridWebView() => throw null!;

		//		private partial void NavigateCore(string url) => throw null!;
		//#endif

		//		public virtual void OnMessageReceived(string message)
		//		{
		//			var messageData = JsonSerializer.Deserialize<WebMessageData>(message);
		//			switch (messageData?.MessageType)
		//			{
		//				case 0: // "raw" message (just a string)
		//					RawMessageReceived?.Invoke(this, new HybridWebViewRawMessageReceivedEventArgs(messageData.MessageContent));
		//					break;
		//				default:
		//					throw new InvalidOperationException($"Unknown message type: {messageData?.MessageType}. Message contents: {messageData?.MessageContent}");
		//			}

		//		}

		//		private sealed class WebMessageData
		//		{
		//			public int MessageType { get; set; }
		//			public string? MessageContent { get; set; }
		//		}

		//		internal static async Task<string?> GetAssetContentAsync(string assetPath)
		//		{
		//			using var stream = await GetAssetStreamAsync(assetPath);
		//			if (stream == null)
		//			{
		//				return null;
		//			}
		//			using var reader = new StreamReader(stream);

		//			var contents = reader.ReadToEnd();

		//			return contents;
		//		}

		//		internal static async Task<Stream?> GetAssetStreamAsync(string assetPath)
		//		{
		//			if (!await FileSystem.AppPackageFileExistsAsync(assetPath))
		//			{
		//				return null;
		//			}
		//			return await FileSystem.OpenAppPackageFileAsync(assetPath);
		//		}
	}
}
