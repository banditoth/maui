﻿using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue2259 : _IssuesUITest
	{
		public Issue2259(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "ListView.ScrollTo crashes app";

		[Test]
		[Category(UITestCategories.ListView)]
		[Category(UITestCategories.Compatibility)]
		[FailsOnIOS]
		public void Issue2259Tests()
		{
			this.IgnoreIfPlatforms([TestDevice.Android, TestDevice.Mac]);

			for (int i = 0; i < 20; i++)
			{
				App.Tap("AddButton");
				App.WaitForElement("Name " + (i + 1).ToString());
				App.Screenshot("Added Cell");
			}
		}
	}
}