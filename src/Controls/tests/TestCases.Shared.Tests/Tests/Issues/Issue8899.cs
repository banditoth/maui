﻿using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue8899 : _IssuesUITest
	{
		const string Go = "Go";
		const string Success = "Success";

		public Issue8899(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "Clearing CollectionView IsGrouped=\"True\" crashes application iOS ";

		[Test]
		[Category(UITestCategories.CollectionView)]
		[Category(UITestCategories.Compatibility)]
		[FailsOnIOS]
		public void ClearingGroupedCollectionViewShouldNotCrash()
		{
			this.IgnoreIfPlatforms([TestDevice.Android, TestDevice.Mac, TestDevice.Windows]);

			App.WaitForElement(Go);
			App.Tap(Go);
			App.WaitForElement(Success);
		}
	}
}