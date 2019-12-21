﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content.Res;
using Android.Views;
using Android.Widget;
using CloudStreamForms.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using static Java.Util.ResourceBundle;
using static CloudStreamForms.Main;

[assembly: ExportRenderer(typeof(TextCell), typeof(CustomTextCellRenderer))]
public class CustomTextCellRenderer : TextCellRenderer
{
    protected override void OnCellPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        base.OnCellPropertyChanged(sender, args);
    }

    protected override Android.Views.View GetCellCore(Cell item, Android.Views.View convertView, Android.Views.ViewGroup parent, Android.Content.Context context)
    {

        var cell = base.GetCellCore(item, convertView, parent, context);
        cell.SetBackgroundColor(new Android.Graphics.Color(20, 20, 20));
       
        return cell;
    }


}