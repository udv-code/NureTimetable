﻿using NureTimetable.UI.ViewModels.Entities;
using Xamarin.Forms;

namespace NureTimetable.UI.Views
{
    public partial class AddTimetablePage : TabbedPage
    {
        public AddTimetablePage()
        {
            InitializeComponent();
            BindingContext = new AddTimetableViewModel();
        }
    }
}