﻿using NureTimetable.Core.Localization;
using NureTimetable.DAL;
using NureTimetable.DAL.Models.Local;
using NureTimetable.UI.Helpers;
using NureTimetable.UI.ViewModels.Core;
using NureTimetable.UI.ViewModels.Lessons.LessonSettings;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace NureTimetable.UI.ViewModels.Lessons.ManageLessons
{
    public class ManageLessonsViewModel : BaseViewModel
    {
        #region Variables
        private bool _isNoSourceLayoutVisable;

        private readonly TimetableInfo timetable;

        private string _title;

        private ObservableCollection<LessonViewModel> _lessons;
        #endregion

        #region Properties
        public string Title
        {
            get => _title;
            private set => SetProperty(ref _title, value);
        }

        public bool IsNoSourceLayoutVisable
        {
            get => _isNoSourceLayoutVisable;
            set => SetProperty(ref _isNoSourceLayoutVisable, value);
        }
        
        public ObservableCollection<LessonViewModel> Lessons { get => _lessons; private set => SetProperty(ref _lessons, value); }

        public ICommand PageAppearingCommand { get; }

        public ICommand SaveClickedCommand { get; }
        #endregion

        public ManageLessonsViewModel(INavigation navigation, SavedEntity savedEntity) : base(navigation)
        {
            Title = $"{LN.Lessons}: {savedEntity.Name}";

            timetable = EventsRepository.GetTimetableLocal(savedEntity);
            if (timetable != null)
            {
                Lessons = new ObservableCollection<LessonViewModel>
                (
                    timetable.Lessons()
                        .Select(lesson => timetable.LessonsInfo.FirstOrDefault(li => li.Lesson == lesson) ?? new LessonInfo { Lesson = lesson })
                        .OrderBy(lesson => !timetable.Events.Where(e => e.Start >= DateTime.Now).Any(e => e.Lesson == lesson.Lesson))
                        .ThenBy(lesson => lesson.Lesson.ShortName)
                        .Select(lesson => new LessonViewModel(Navigation, lesson, timetable))
                );
                IsNoSourceLayoutVisable = Lessons.Count == 0;
            }
            else
            {
                IsNoSourceLayoutVisable = true;
            }

            MessagingCenter.Subscribe<LessonSettingsViewModel, LessonInfo>(this, "OneLessonSettingsChanged", (sender, newLessonSettings) =>
            {
                for (int i = 0; i < Lessons.Count; i++)
                {
                    if (Lessons[i].LessonInfo.Lesson == newLessonSettings.Lesson)
                    {
                        Lessons[i].LessonInfo = newLessonSettings;
                        Lessons[i].LessonInfo.Settings.NotifyChanged();
                        break;
                    }
                }
            });

            PageAppearingCommand = CommandHelper.CreateCommand(PageAppearing);
            SaveClickedCommand = CommandHelper.CreateCommand(SaveClicked);
        }

        private async Task PageAppearing()
        {
            if (Lessons == null)
            {
                await App.Current.MainPage.DisplayAlert(LN.LessonsManagement, LN.AtFirstLoadTimetable, LN.Ok);
                Navigation.PopAsync();
            }
        }
        
        private async Task SaveClicked()
        {
            if (Lessons == null)
            {
                await App.Current.MainPage.DisplayAlert(LN.LessonsManagement, LN.AtFirstLoadTimetable, LN.Ok);
                return;
            }

            EventsRepository.UpdateLessonsInfo(timetable.Entity, Lessons.Select(l => l.LessonInfo).ToList());
            await App.Current.MainPage.DisplayAlert(LN.SavingSettings, string.Format(LN.EntityLessonSettingsSaved, timetable.Entity.Name), LN.Ok);
            Navigation.PopAsync();
        }
    }
}