﻿using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue1355 : _IssuesUITest
	{
		const string Success = "Success";

		public Issue1355(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "Setting Main Page in quick succession causes crash on Android";

		[Test]
		[Category(UITestCategories.LifeCycle)]
		[Category(UITestCategories.Compatibility)]
		[FailsOnAndroid]
		public void SwitchMainPageOnAppearing()
		{
			this.IgnoreIfPlatforms([TestDevice.iOS, TestDevice.Mac, TestDevice.Windows]);

			// Without the fix, this would crash. If we're here at all, the test passed.
			App.WaitForNoElement(Success);
		}
	}
}