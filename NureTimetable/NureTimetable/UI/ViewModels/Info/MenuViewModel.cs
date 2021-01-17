﻿using NureTimetable.BL;
using NureTimetable.Core.Localization;
using NureTimetable.Core.Models.Consts;
using NureTimetable.Core.Models.InterplatformCommunication;
using NureTimetable.Core.Models.Settings;
using NureTimetable.DAL;
using NureTimetable.UI.Helpers;
using NureTimetable.UI.Themes;
using NureTimetable.UI.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using AppTheme = NureTimetable.Core.Models.Settings.AppTheme;

namespace NureTimetable.UI.ViewModels.Info
{
    public class MenuViewModel : BaseViewModel
    {
        #region Properties
        public bool IsDebugModeActive
        {
            get => App.IsDebugMode;
            set
            {
                App.IsDebugMode = value;
                OnPropertyChanged();
            }
        }

        public LocalizedString AppVersion { get; } = new(() => string.Format(LN.Version, AppInfo.VersionString));

        private bool langIsRestartRequired = false;
        public string AppLanguageName => 
            languageMapping.Single(m => m.value == SettingsRepository.Settings.Language).name() + 
            (langIsRestartRequired ? $" ({LN.RestartRequired})" : string.Empty);

        public string AppThemeName => themeMapping.Single(m => m.value == SettingsRepository.Settings.Theme).name();

        public string DefaultCalendarName => calendarMapping?.Single(m => m.id == SettingsRepository.Settings.DefaultCalendarId).name() ?? LN.Wait;

        public IAsyncCommand PageAppearingCommand { get; }
        public IAsyncCommand<string> NavigateUriCommand { get; }
        public Command ToggleDebugModeCommand { get; }
        public IAsyncCommand OpenDonatePageCommand { get; }
        public IAsyncCommand ChangeThemeCommand { get; }
        public IAsyncCommand ChangeLanguageCommand { get; }
        public IAsyncCommand ChangeDefaultCalendarCommand { get; }
        #endregion

        #region Setting mappings
        List<(Func<string> name, AppLanguage value)> languageMapping { get; } = new()
        {
            (() => LN.FollowSystem, AppLanguage.FollowSystem),
            (() => LN.EnglishLanguage, AppLanguage.English),
            (() => LN.RussianLanguage, AppLanguage.Russian),
            (() => LN.UkrainianLanguage, AppLanguage.Ukrainian),
        };

        List<(Func<string> name, AppTheme value)> themeMapping { get; } = new()
        {
            (() => LN.FollowSystem, AppTheme.FollowSystem),
            (() => LN.LightTheme, AppTheme.Light),
            (() => LN.DarkTheme, AppTheme.Dark),
        };

        List<(Func<string> name, string id)> calendarMapping;
        #endregion

        public MenuViewModel()
        {
            PageAppearingCommand = CommandHelper.Create(PageAppearing);
            NavigateUriCommand = CommandHelper.Create<string>(async url => await Launcher.OpenAsync(new Uri(url)));
            ToggleDebugModeCommand = CommandHelper.Create(() => IsDebugModeActive = !IsDebugModeActive);
            OpenDonatePageCommand = CommandHelper.Create(async () => await Navigation.PushAsync(new DonatePage()));
            ChangeThemeCommand = CommandHelper.Create(ChangeTheme);
            ChangeLanguageCommand = CommandHelper.Create(ChangeLanguage);
            ChangeDefaultCalendarCommand = CommandHelper.Create(ChangeDefaultCalendar);
            
            MessagingCenter.Subscribe<Application, AppTheme>(Application.Current, MessageTypes.ThemeChanged, (sender, theme) => OnPropertyChanged(nameof(AppThemeName)));
            LocalizationResourceManager.Current.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(AppThemeName));
                OnPropertyChanged(nameof(AppLanguageName));
                OnPropertyChanged(nameof(DefaultCalendarName));
            };
        }

        private async Task PageAppearing()
        {
            await UpdateDefaultCalendarName(false);
        }

        public async Task ChangeSetting<T>(string name, List<(Func<string> name, T value)> mapping, T currectValue, Action<T> applyNewValue)
        {
            string selectedName = await Shell.Current.DisplayActionSheet(name, LN.Cancel, null, mapping.Select(m => m.name() ?? string.Empty).ToArray());
            if (selectedName is null)
                return;

            if (selectedName == string.Empty)
            {
                selectedName = null;
            }

            T selectedValue = mapping.Single(m => m.name() == selectedName).value;
            if (currectValue?.Equals(selectedValue) == true)
            {
                return;
            }

            applyNewValue(selectedValue);
        }

        public Task ChangeTheme()
        {
            return ChangeSetting
            (
                LN.Theme,
                themeMapping,
                SettingsRepository.Settings.Theme,
                newTheme =>
                {
                    SettingsRepository.Settings.Theme = newTheme;
                    ThemeHelper.SetAppTheme(newTheme);
                    OnPropertyChanged(nameof(AppThemeName));
                }
            );
        }

        public Task ChangeLanguage()
        {
            return ChangeSetting
            (
                LN.Language,
                languageMapping,
                SettingsRepository.Settings.Language,
                newLanguage =>
                {
                    SettingsRepository.Settings.Language = newLanguage;

                    langIsRestartRequired = true;
                    LocalizationResourceManager.Current.SetCulture(newLanguage == AppLanguage.FollowSystem ? CultureInfo.CurrentCulture : new CultureInfo((int)newLanguage));

                    // TODO: Remove when https://github.com/xamarin/XamarinCommunityToolkit/issues/745 is closed
                    var activityManager = DependencyService.Get<IActivityManager>();
                    activityManager.Recreate();
                }
            );
        }

        private async Task UpdateDefaultCalendarName(bool requestPermissionIfNeeded)
        {
            calendarMapping = new() 
            { 
                (() => LN.AskEveryTime, null) 
            };

            if (requestPermissionIfNeeded || await CalendarService.CheckPermissions())
            {
                var calendars = await CalendarService.GetCalendars();
                if (calendars is not null)
                {
                    calendarMapping.AddRange(calendars.Select(c => ((Func<string>)(() => c.Name), c.ExternalID)));
                }
            }

            OnPropertyChanged(nameof(DefaultCalendarName));
        }

        public async Task ChangeDefaultCalendar()
        {
            await UpdateDefaultCalendarName(true);
            await ChangeSetting
            (
                LN.DefaultCalendar,
                calendarMapping,
                SettingsRepository.Settings.DefaultCalendarId,
                newCalendar => 
                {
                    SettingsRepository.Settings.DefaultCalendarId = newCalendar;
                    OnPropertyChanged(nameof(DefaultCalendarName));
                }
            );
        }
    }
}