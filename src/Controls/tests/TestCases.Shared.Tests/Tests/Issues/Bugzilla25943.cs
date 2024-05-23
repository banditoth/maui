﻿using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Bugzilla25943 : _IssuesUITest
	{
		public Bugzilla25943(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "[Android] TapGestureRecognizer does not work with a nested StackLayout";

		/*	
		const string InnerLayout = "innerlayout";
		const string OuterLayout = "outerlayout";
		const string Success = "Success";

		[Test]
		[Category(UITestCategories.LifeCycle)]
		[Category(UITestCategories.Compatibility)]
		[FailsOnAndroid]
		[FailsOnIOS]
		public void VerifyNestedStacklayoutTapsBubble()
		{
			App.WaitForElement(InnerLayout);
			App.Tap(InnerLayout);

			App.WaitForElement(OuterLayout);
			App.Tap(OuterLayout);

			App.WaitForNoElement(Success);
		}
		*/
	}
}