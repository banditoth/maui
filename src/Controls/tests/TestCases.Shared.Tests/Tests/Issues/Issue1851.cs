﻿using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue1851 : _IssuesUITest
	{
		public Issue1851(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "ObservableCollection in ListView gets Index out of range when removing item";

		[Test]
		[Category(UITestCategories.ListView)]
		[Category(UITestCategories.Compatibility)]
		[FailsOnIOS]
		public void Issue1851Test()
		{
			this.IgnoreIfPlatforms([TestDevice.Mac, TestDevice.Windows]);

			App.WaitForElement("btn");
			App.Tap("btn");
			App.WaitForElement("btn");
			App.Tap("btn");
			App.WaitForElement("btn");
		}
	}
}
