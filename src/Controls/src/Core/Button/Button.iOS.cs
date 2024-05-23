﻿#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using UIKit;
using CoreGraphics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Maui.Controls
{
	public partial class Button
	{
		CGSize _originalImageSize = CGSize.Empty;

		protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
		{
			var result = base.MeasureOverride(widthConstraint, heightConstraint);

			var button = this;
			var platformButton = Handler?.PlatformView as UIButton;

			if (button is null || platformButton is null)
			{
				return result;
			}

			// Calculate the size the button wants to use for its current content
			var image = platformButton.CurrentImage;

			// Save the original image size for later image resizing
			if (image is not null && _originalImageSize == CGSize.Empty)
			{
				_originalImageSize = image.Size;
			}

			var layout = button.ContentLayout;
			var spacing = (nfloat)layout.Spacing;

			var padding = button.Padding;
			if (padding.IsNaN)
				padding = ButtonHandler.DefaultPadding;

			var titleRect = platformButton.GetTitleBoundingRect(widthConstraint, heightConstraint);

			var buttonContentHeight =
				(nfloat)Math.Max(titleRect.Height, _originalImageSize.Height)
				+ (nfloat)padding.Top
				+ (nfloat)padding.Bottom;

			var buttonContentWidth =
				(nfloat)Math.Max(titleRect.Width, _originalImageSize.Width)
				+ (nfloat)padding.Left
				+ (nfloat)padding.Right;

			if (image is not null && !string.IsNullOrEmpty(platformButton.CurrentTitle))
			{
				if (layout.Position == ButtonContentLayout.ImagePosition.Top || layout.Position == ButtonContentLayout.ImagePosition.Bottom)
				{
					buttonContentHeight += spacing;
					buttonContentHeight += (nfloat)Math.Min(titleRect.Height, _originalImageSize.Height);
				}

				if (layout.Position == ButtonContentLayout.ImagePosition.Left || layout.Position == ButtonContentLayout.ImagePosition.Right)
				{
					buttonContentWidth += spacing;
					buttonContentWidth += (nfloat)Math.Min(titleRect.Width, _originalImageSize.Width);
				}
			}

			// return either the size from its children or the requested button content size or use the explicit height or width if set
			return new Size(button.WidthRequest == -1 ? Math.Max(result.Width, buttonContentWidth) : button.WidthRequest,
				button.HeightRequest == -1 ? Math.Max(result.Height, buttonContentHeight) : button.HeightRequest);
		}

		protected override Size ArrangeOverride(Rect bounds)
		{
			var result = base.ArrangeOverride(bounds);

			var button = this;
			var platformButton = Handler?.PlatformView as UIButton;

			if (button is null || platformButton is null)
			{
				return result;
			}

			if (platformButton.Bounds.Width == 0)
			{
				return result;
			}

			var imageInsets = new UIEdgeInsets();
			var titleInsets = new UIEdgeInsets();

			var layout = button.ContentLayout;
			var spacing = (nfloat)layout.Spacing;
			var borderWidth = button.BorderWidth == -1 ? 0 : button.BorderWidth;

			var image = platformButton.CurrentImage;

			// if the image is too large then we just position at the edge of the button
			// depending on the position the user has picked
			// This makes the behavior consistent with android
			var contentMode = UIViewContentMode.Center;

			var padding = button.Padding;
			if (padding.IsNaN)
				padding = ButtonHandler.DefaultPadding;

			// If the button's image takes up too much space, we will want to hide the title
			var hidesTitle = false;

			if (image is not null && !string.IsNullOrEmpty(platformButton.CurrentTitle))
			{
				// Resize the image if necessary and then update the image variable
				if (ResizeImageIfNecessary(platformButton, button, image, spacing, padding, borderWidth, result, _originalImageSize))
				{
					image = platformButton.CurrentImage;
				}

				// TODO: Do not use the title label as it is not yet updated and
				//       if we move the image, then we technically have more
				//       space and will require a new layout pass.
				var titleRect = platformButton.GetTitleBoundingRect(platformButton.Bounds.Width, platformButton.Bounds.Height);
				var titleWidth = titleRect.Width;
				var titleHeight = titleRect.Height;
				var imageWidth = image.Size.Width;
				var imageHeight = image.Size.Height;
				var buttonWidth = platformButton.Bounds.Width;
				var buttonHeight = platformButton.Bounds.Height;
				var sharedSpacing = spacing / 2;

				// The titleWidth will include the part of the title that is potentially truncated. Let's figure out the max width of the title in the button for our calculations.
				// Note: we do not calculate spacing in maxTitleWidth since the original button laid out by iOS will not contain the spacing in the measurements.
				var maxTitleWidth = platformButton.Bounds.Width - (imageWidth + (nfloat)padding.Left + (nfloat)padding.Right);
				var titleWidthMove = (nfloat)Math.Min(maxTitleWidth, titleWidth);

				// These are just used to shift the image and title to center
				// Which makes the later math easier to follow
				imageInsets.Left += titleWidthMove / 2;
				imageInsets.Right -= titleWidthMove / 2;
				titleInsets.Left -= imageWidth / 2;
				titleInsets.Right += imageWidth / 2;

				if (layout.Position == ButtonContentLayout.ImagePosition.Top)
				{
					if (imageHeight > buttonHeight)
					{
						contentMode = UIViewContentMode.Top;
					}

					imageInsets.Top -= (titleHeight / 2) + sharedSpacing;
					imageInsets.Bottom += (titleHeight / 2) + sharedSpacing;

					titleInsets.Top += (imageHeight / 2) + sharedSpacing;
					titleInsets.Bottom -= (imageHeight / 2) + sharedSpacing;
				}
				else if (layout.Position == ButtonContentLayout.ImagePosition.Bottom)
				{
					if (imageHeight > buttonHeight)
					{
						contentMode = UIViewContentMode.Bottom;
					}

					imageInsets.Top += (titleHeight / 2) + sharedSpacing;
					imageInsets.Bottom -= (titleHeight / 2) + sharedSpacing;

					titleInsets.Top -= (imageHeight / 2) + sharedSpacing;
					titleInsets.Bottom += (imageHeight / 2) + sharedSpacing;
				}
				else if (layout.Position == ButtonContentLayout.ImagePosition.Left)
				{
					if (imageWidth > buttonWidth)
					{
						contentMode = UIViewContentMode.Left;
					}

					imageInsets.Left -= (titleWidthMove / 2) + sharedSpacing;
					imageInsets.Right += (titleWidthMove / 2) + sharedSpacing;

					titleInsets.Left += (imageWidth / 2) + sharedSpacing;
					titleInsets.Right -= (imageWidth / 2) + sharedSpacing;

				}
				else if (layout.Position == ButtonContentLayout.ImagePosition.Right)
				{
					if (imageWidth > buttonWidth)
					{
						contentMode = UIViewContentMode.Right;
					}

					imageInsets.Left += (titleWidthMove / 2) + sharedSpacing;
					imageInsets.Right -= (titleWidthMove / 2) + sharedSpacing;

					titleInsets.Left -= (imageWidth / 2) + sharedSpacing;
					titleInsets.Right += (imageWidth / 2) + sharedSpacing;
				}
			}

			// If we just have an image, we can still resize it here
			else if (image is not null)
			{
				ResizeImageIfNecessary(platformButton, button, image, 0, padding, borderWidth, result, _originalImageSize);
			}

			platformButton.ImageView.ContentMode = contentMode;

			// This is used to match the behavior between platforms.
			// If the image is too big then we just hide the label because
			// the image is pushing the title out of the visible view.
			// We can't use insets because then the title shows up outside the
			// bounds of the UIButton. We could set the UIButton to clip bounds
			// but that feels like it might cause confusing side effects
			if (contentMode == UIViewContentMode.Center)
				platformButton.TitleLabel.Layer.Hidden = false;
			else
				platformButton.TitleLabel.Layer.Hidden = true;

			platformButton.UpdatePadding(button);

#pragma warning disable CA1416, CA1422
			if (platformButton.ImageEdgeInsets != imageInsets ||
				platformButton.TitleEdgeInsets != titleInsets)
			{
				platformButton.ImageEdgeInsets = imageInsets;
				platformButton.TitleEdgeInsets = titleInsets;
				platformButton.Superview?.SetNeedsLayout();
				platformButton.Superview?.LayoutIfNeeded();
				return result;
			}
#pragma warning restore CA1416, CA1422

			var titleRectHeight = platformButton.GetTitleBoundingRect(platformButton.Bounds.Width, platformButton.Bounds.Height).Height;

			var buttonContentHeight =
				+ (nfloat)Math.Max(titleRectHeight, platformButton.CurrentImage?.Size.Height ?? 0)
				+ (nfloat)padding.Top
				+ (nfloat)padding.Bottom
				+ (nfloat)borderWidth * 2;

			if (image is not null && !string.IsNullOrEmpty(platformButton.CurrentTitle))
			{
				if (layout.Position == ButtonContentLayout.ImagePosition.Top || layout.Position == ButtonContentLayout.ImagePosition.Bottom)
				{
					if (!hidesTitle)
					{
						buttonContentHeight += spacing;
						buttonContentHeight += (nfloat)Math.Min(titleRectHeight, platformButton.CurrentImage?.Size.Height ?? 0);
					}
					// If the title is hidden, we don't need to add the spacing or the title to this measurement
					else
					{
						if (titleRectHeight > platformButton.CurrentImage.Size.Height)
						{
							buttonContentHeight -= titleRectHeight;
							buttonContentHeight += platformButton.CurrentImage.Size.Height;
						}
					}
				}

#pragma warning disable CA1416, CA1422

				var maxButtonHeight = Math.Min(buttonContentHeight, result.Height);

				// If the button's content is larger than the button, we need to adjust the ContentEdgeInsets.
				// Apply a small buffer to the image size comparison since iOS can return a size that is off by a fraction of a pixel
				if (maxButtonHeight - button.Height > 1 && button.HeightRequest == -1)
				{
					var contentInsets = platformButton.ContentEdgeInsets;

					var additionalVerticalSpace = (maxButtonHeight - button.Height) / 2;

					platformButton.ContentEdgeInsets = new UIEdgeInsets(
						(nfloat)(additionalVerticalSpace + (nfloat)padding.Top),
						contentInsets.Left,
						(nfloat)(additionalVerticalSpace + (nfloat)padding.Bottom),
						contentInsets.Right);

					platformButton.Superview?.SetNeedsLayout();
					platformButton.Superview?.LayoutIfNeeded();
				}
#pragma warning restore CA1416, CA1422
			}

			return result;
		}

		static bool ResizeImageIfNecessary(UIButton platformButton, Button button, UIImage image, nfloat spacing, Thickness padding, double borderWidth, Size bounds, CGSize originalImageSize)
		{
			var currentImageWidth = image.Size.Width;
			var currentImageHeight = image.Size.Height;

			nfloat availableWidth = (nfloat)bounds.Width;
			nfloat availableHeight = (nfloat)bounds.Height;

			var additionalHorizontalSpace = (nfloat)padding.Left + (nfloat)padding.Right + ((nfloat)borderWidth * 2);
			var additionalVerticalSpace = (nfloat)padding.Top + (nfloat)padding.Bottom + ((nfloat)borderWidth * 2);

			// Apply a small buffer to the image size comparison since iOS can return a size that is off by a fraction of a pixel.
			var buffer = 0.1;

			if (bounds != Size.Zero && (!double.IsNaN(bounds.Height) || !double.IsNaN(bounds.Width)))
			{
				var contentWidth = (nfloat)bounds.Width - additionalHorizontalSpace;

				if (currentImageWidth - contentWidth > buffer)
				{
					availableWidth = contentWidth;
				}

				var contentHeight = (nfloat)bounds.Height - additionalVerticalSpace;
				if (currentImageHeight - contentHeight > buffer)
				{
					availableHeight = contentHeight;
				}
			}

			// make sure we have values greater than 0
			availableWidth = (nfloat)Math.Max(availableWidth, 0.1f);
			availableHeight = (nfloat)Math.Max(availableHeight, 0.1f);

			try
			{
				// if the image is too large then we will size it smaller
				if (currentImageHeight - availableHeight > buffer || currentImageWidth - availableWidth > buffer)
				{
					image = ResizeImageSource(image, availableWidth, availableHeight, originalImageSize);
				}
				// if the image is already sized down but now has more space, we will size it up no more than the original image size
				else if (availableHeight - additionalVerticalSpace - currentImageHeight > buffer
					&& availableWidth - additionalHorizontalSpace - currentImageWidth > buffer
					&& currentImageHeight != originalImageSize.Height
					&& currentImageWidth != originalImageSize.Width)
				{
					image = ResizeImageSource(image, availableWidth, availableHeight, originalImageSize, true);
				}
				else
				{
					return false;
				}

				image = image?.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);

				platformButton.SetImage(image, UIControlState.Normal);

				platformButton.Superview?.SetNeedsLayout();

				return true;
			}
			catch (Exception)
			{
				button.Handler.MauiContext?.CreateLogger<ButtonHandler>()?.LogWarning("Can not load Button ImageSource");
			}

			return false;
		}

		static UIImage ResizeImageSource(UIImage sourceImage, nfloat maxWidth, nfloat maxHeight, CGSize originalImageSize, bool shouldScaleUp = false)
		{
			if (sourceImage is null || sourceImage.CGImage is null)
				return null;

			maxWidth = (nfloat)Math.Min(maxWidth, originalImageSize.Width);
			maxHeight = (nfloat)Math.Min(maxHeight, originalImageSize.Height);

			var sourceSize = sourceImage.Size;

			float maxResizeFactor = (float)Math.Min(maxWidth / sourceSize.Width, maxHeight / sourceSize.Height);

			if (maxResizeFactor > 1 && !shouldScaleUp)
				return sourceImage;

			return UIImage.FromImage(sourceImage.CGImage, sourceImage.CurrentScale / maxResizeFactor, sourceImage.Orientation);
		}

		public static void MapText(ButtonHandler handler, Button button) =>
			MapText((IButtonHandler)handler, button);

		public static void MapLineBreakMode(IButtonHandler handler, Button button)
		{
			handler.PlatformView?.UpdateLineBreakMode(button);
		}

		private static void MapPadding(IButtonHandler handler, Button button)
		{
			handler.PlatformView.UpdatePadding(button);
		}

		public static void MapText(IButtonHandler handler, Button button)
		{
			handler.PlatformView?.UpdateText(button);
		}

		internal static void MapBorderWidth(IButtonHandler handler, Button button)
		{
			handler.PlatformView?.UpdateContentLayout(button);
		}
	}
}
