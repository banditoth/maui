﻿using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue1900 : _IssuesUITest
	{
		public Issue1900(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "Xamarin ios ListView ObservableCollection<myClass>. Collection.Add() throwing 'Index # is greater than the number of rows #' exception";

		[Test]
		[Category(UITestCategories.ListView)]
		[Category(UITestCategories.Compatibility)]
		[FailsOnIOS]
		public void Issue1900Test()
		{
			this.IgnoreIfPlatforms([TestDevice.Android, TestDevice.Mac, TestDevice.Windows]);

			App.WaitForElement("ListView");
		}
	}
}