﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Controls.Sample
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();

			Content = new VerticalStackLayout(){
				new Button()
				{
					Text = "Click Me",
					ImageSource = "groceries.png",
					ContentLayout= new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Right,100)
				}
			};
		}
	}
}