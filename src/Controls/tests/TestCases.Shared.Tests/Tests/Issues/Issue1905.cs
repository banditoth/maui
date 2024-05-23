﻿using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue1905 : _IssuesUITest
	{
		public Issue1905(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "AlertView doesn't scroll when text is to large";
		public override bool ResetMainPage => false;

		[Test]
		[Category(UITestCategories.DisplayAlert)]
		[Category(UITestCategories.Compatibility)]
		[FailsOnAndroid]
		public void TestIssue1905RefreshShows()
		{
			this.IgnoreIfPlatforms([TestDevice.iOS, TestDevice.Mac, TestDevice.Windows]);

			// wait for test to load
			App.WaitForElement("btnRefresh");
			App.Screenshot("Should show refresh control");

			// wait for test to finish so it doesn't keep working
			// in the background and break the next test
			App.WaitForElement("data refreshed");
		}
	}
}