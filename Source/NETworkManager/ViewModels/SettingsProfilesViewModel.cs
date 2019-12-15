﻿using MahApps.Metro.Controls.Dialogs;
using NETworkManager.Models.Profile;
using NETworkManager.Models.Settings;
using NETworkManager.Utilities;
using NETworkManager.Views;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace NETworkManager.ViewModels
{
    public class SettingsProfilesViewModel : ViewModelBase
    {
        #region Variables
        private readonly IDialogCoordinator _dialogCoordinator;

        private readonly bool _isLoading;

        public bool IsPortable => ConfigurationManager.Current.IsPortable;

        private string _location;
        public string Location
        {
            get => _location;
            set
            {
                if (value == _location)
                    return;

                _location = value;
                OnPropertyChanged();
            }
        }

        private bool _movingFiles;
        public bool MovingFiles
        {
            get => _movingFiles;
            set
            {
                if (value == _movingFiles)
                    return;

                _movingFiles = value;
                OnPropertyChanged();
            }
        }

        private ICollectionView _profileFiles;
        public ICollectionView ProfileFiles
        {
            get => _profileFiles;
            set
            {
                if (value == _profileFiles)
                    return;

                _profileFiles = value;
                OnPropertyChanged();
            }
        }

        private ProfileFileInfo _selectedProfileFile;
        public ProfileFileInfo SelectedProfileFile
        {
            get => _selectedProfileFile;
            set
            {
                if (Equals(value, _selectedProfileFile))
                    return;

                _selectedProfileFile = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Constructor, LoadSettings
        public SettingsProfilesViewModel(IDialogCoordinator instance)
        {
            _isLoading = true;

            _dialogCoordinator = instance;

            ProfileFiles = new CollectionViewSource { Source = ProfileManager.ProfileFiles }.View;
            ProfileFiles.SortDescriptions.Add(new SortDescription(nameof(ProfileFileInfo.Name), ListSortDirection.Ascending));

            LoadSettings();

            _isLoading = false;
        }

        private void LoadSettings()
        {
            Location = ProfileManager.GetProfilesLocation();
        }
        #endregion

        #region ICommands & Actions
        public ICommand BrowseLocationFolderCommand => new RelayCommand(p => BrowseLocationFolderAction());

        private void BrowseLocationFolderAction()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (Directory.Exists(Location))
                    dialog.SelectedPath = Location;

                var dialogResult = dialog.ShowDialog();

                if (dialogResult == System.Windows.Forms.DialogResult.OK)
                    Location = dialog.SelectedPath;
            }
        }

        public ICommand OpenLocationCommand => new RelayCommand(p => OpenLocationAction());

        private static void OpenLocationAction()
        {
            Process.Start("explorer.exe", ProfileManager.GetProfilesLocation());
        }

        public ICommand ChangeLocationCommand => new RelayCommand(p => ChangeLocationAction());

        private async void ChangeLocationAction()
        {
            MovingFiles = true;

            // Get files from new location and check if there are files with the same name
            var containsFile = Directory.GetFiles(Location).Where(x => Path.GetExtension(x) == ProfileManager.ProfilesFileExtension).Count() > 0;

            var copyFiles = false;

            // Check if settings file exists in new location
            if (containsFile)
            {
                var settings = AppearanceManager.MetroDialog;

                settings.AffirmativeButtonText = Resources.Localization.Strings.Overwrite;
                settings.NegativeButtonText = Resources.Localization.Strings.Cancel;
                settings.FirstAuxiliaryButtonText = Resources.Localization.Strings.UseOther;
                settings.DefaultButtonFocus = MessageDialogResult.FirstAuxiliary;

                var result = await _dialogCoordinator.ShowMessageAsync(this, Resources.Localization.Strings.Overwrite, Resources.Localization.Strings.OverwriteSettingsInTheDestinationFolder, MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, AppearanceManager.MetroDialog);

                switch (result)
                {
                    case MessageDialogResult.Negative:
                        MovingFiles = false;
                        return;
                    case MessageDialogResult.Affirmative:
                        copyFiles = true;
                        break;
                }
            }

            if (copyFiles)
            {
                try
                {
                    await ProfileManager.MoveProfilesAsync(Location);

                    // Show the user some awesome animation to indicate we are working on it :)
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    var settings = AppearanceManager.MetroDialog;

                    settings.AffirmativeButtonText = Resources.Localization.Strings.OK;

                    await _dialogCoordinator.ShowMessageAsync(this, Resources.Localization.Strings.Error, ex.Message, MessageDialogStyle.Affirmative, settings);
                }
            }

            SettingsManager.Current.Profiles_CustomProfilesLocation = Location;

            Location = string.Empty;
            Location = SettingsManager.Current.Profiles_CustomProfilesLocation;

            MovingFiles = false;
        }

        public ICommand RestoreDefaultProfilesLocationCommand => new RelayCommand(p => RestoreDefaultProfilesLocationAction());

        private void RestoreDefaultProfilesLocationAction()
        {
            Location = ProfileManager.GetDefaultProfilesLocation();
        }

        public ICommand AddProfileFileCommand => new RelayCommand(p => AddProfileFileAction());

        private async void AddProfileFileAction()
        {
            var customDialog = new CustomDialog
            {
                Title = Resources.Localization.Strings.AddProfileFile
            };

            var profileFileViewModel = new ProfileFileViewModel(async instance =>
            {
                await _dialogCoordinator.HideMetroDialogAsync(this, customDialog);

                //ProfileManager.AddProfileFile(instance.Name);
            }, async instance =>
            {
                await _dialogCoordinator.HideMetroDialogAsync(this, customDialog);
            });

            customDialog.Content = new ProfileFileDialog
            {
                DataContext = profileFileViewModel
            };

            await _dialogCoordinator.ShowMetroDialogAsync(this, customDialog);            
        }

        public ICommand EditProfileFileCommand => new RelayCommand(p => EditProfileFileAction());

        private async void EditProfileFileAction()
        {
            var customDialog = new CustomDialog
            {
                Title = Resources.Localization.Strings.EditProfileFile
            };

            var profileFileViewModel = new ProfileFileViewModel(async instance =>
            {
                await _dialogCoordinator.HideMetroDialogAsync(this, customDialog);

                //ProfileManager.EditProfileFile(SelectedProfile, instance.Name);
            }, async instance =>
            {
                await _dialogCoordinator.HideMetroDialogAsync(this, customDialog);
            }, SelectedProfileFile);

            customDialog.Content = new ProfileFileDialog
            {
                DataContext = profileFileViewModel
            };

            await _dialogCoordinator.ShowMetroDialogAsync(this, customDialog);
        }

        public ICommand RemoveProfileFileCommand => new RelayCommand(p => RemoveProfileFileAction());

        private void RemoveProfileFileAction()
        {
            
            //ProfileManager.RemoveProfileFile(SelectedProfileFile);
        }

        /*
        public ICommand BrowseImportFileCommand => new RelayCommand(p => BrowseFileAction());

        private void BrowseFileAction()
        {
            using (var openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = GlobalStaticConfiguration.ZipFileExtensionFilter
            })
            {
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    ImportFilePath = openFileDialog.FileName;
            }
        }

        public ICommand ImportSettingsCommand => new RelayCommand(p => ImportSettingsAction());

        private async void ImportSettingsAction()
        {
            var settings = AppearanceManager.MetroDialog;

            settings.AffirmativeButtonText = Resources.Localization.Strings.Continue;
            settings.NegativeButtonText = Resources.Localization.Strings.Cancel;

            settings.DefaultButtonFocus = MessageDialogResult.Affirmative;
                        
            if (await _dialogCoordinator.ShowMessageAsync(this, Resources.Localization.Strings.AreYouSure, Resources.Localization.Strings.SelectedSettingsAreOverwrittenAndApplicationIsRestartedAfterwards, MessageDialogStyle.AffirmativeAndNegative, settings) != MessageDialogResult.Affirmative)
                return;

            try
            {
                SettingsManager.Import(ImportFilePath);

                // Restart the application
                ConfigurationManager.Current.ForceRestart = true;
                CloseAction();
            }
            catch (Exception ex)
            {
                ImportStatusMessage = string.Format(Resources.Localization.Strings.ClouldNotImportFileSeeErrorMessageXX, ex.Message);
                DisplayImportStatusMessage = true;
            }
        }

        public ICommand ExportSettingsCommand => new RelayCommand(p => ExportSettingsAction());

        private void ExportSettingsAction()
        {
            DisplayExportStatusMessage = false;

            using (var saveFileDialog = new System.Windows.Forms.SaveFileDialog()
            {
                Filter = GlobalStaticConfiguration.ZipFileExtensionFilter,
                FileName = $"{AssemblyManager.Current.Name}_{Resources.Localization.Strings.Settings}_{Resources.Localization.Strings.Backup}#{TimestampHelper.GetTimestamp()}.zip"
            })
            {
                if (saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                try
                {
                    SettingsManager.Export(saveFileDialog.FileName);

                    ExportStatusMessage = string.Format(Resources.Localization.Strings.FileExportedToXX, saveFileDialog.FileName);
                    DisplayExportStatusMessage = true;
                }
                catch (Exception ex)
                {
                    ExportStatusMessage = string.Format(Resources.Localization.Strings.ClouldNotExportFileSeeErrorMessageXX, ex.Message);
                    DisplayExportStatusMessage = true;
                }
            }
        }

        public ICommand ResetSettingsCommand => new RelayCommand(p => ResetSettingsAction());

        public async void ResetSettingsAction()
        {
            var settings = AppearanceManager.MetroDialog;

            settings.AffirmativeButtonText = Resources.Localization.Strings.Continue;
            settings.NegativeButtonText = Resources.Localization.Strings.Cancel;

            settings.DefaultButtonFocus = MessageDialogResult.Affirmative;

            var message = Resources.Localization.Strings.SelectedSettingsAreReset;

            message += Environment.NewLine + Environment.NewLine + $"* {Resources.Localization.Strings.TheSettingsLocationIsNotAffected}";
            message += Environment.NewLine + $"* {Resources.Localization.Strings.ApplicationIsRestartedAfterwards}";

            if (await _dialogCoordinator.ShowMessageAsync(this, Resources.Localization.Strings.AreYouSure, message, MessageDialogStyle.AffirmativeAndNegative, settings) != MessageDialogResult.Affirmative)
                return;

            SettingsManager.Reset();

            message = Resources.Localization.Strings.SettingsSuccessfullyReset;
            message += Environment.NewLine + Environment.NewLine + Resources.Localization.Strings.TheApplicationWillBeRestarted;

            await _dialogCoordinator.ShowMessageAsync(this, Resources.Localization.Strings.Success, message, MessageDialogStyle.Affirmative, settings);

            // Restart the application
            ConfigurationManager.Current.ForceRestart = true;
            CloseAction();
        }
        */
        #endregion

        #region Methods
        public void SaveAndCheckSettings()
        {
            // Save everything
            if (ProfileManager.ProfilesChanged)
                ProfileManager.Save();

            // Check if files exist
            //SettingsExists = File.Exists(SettingsManager.GetSettingsFilePath());
        }

        public void SetLocationPathFromDragDrop(string path)
        {
            Location = path;

            OnPropertyChanged(nameof(Location));
        }

        /*
        public void SetImportFilePathFromDragDrop(string filePath)
        {
            ImportFilePath = filePath;

            OnPropertyChanged(nameof(ImportFilePath));
        }
        */
        #endregion
    }
}