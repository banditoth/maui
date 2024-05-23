﻿using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue9929 : _IssuesUITest
	{
		public Issue9929(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "[Bug] NSInternalInconsistencyException when trying to run XamarinTV on iOS";

		public override bool ResetMainPage => false;

		[Test]
		[Category(UITestCategories.CollectionView)]
		[Category(UITestCategories.Compatibility)]
		[FailsOnIOS]
		public void Issue3884Test()
		{
			this.IgnoreIfPlatforms([TestDevice.Android, TestDevice.Mac, TestDevice.Windows]);

			App.WaitForElement("entryUpdate_Spacing");
			App.Tap("entryUpdate_Spacing");
			App.ClearText("entryUpdate_Spacing");
			App.EnterText("entryUpdate_Spacing", "0,500");
			App.Tap("btnUpdate_Spacing");

			// If it hasn't crashed, we should still be able to find this
			App.WaitForElement("entryUpdate_Spacing");
		}
	}
}
