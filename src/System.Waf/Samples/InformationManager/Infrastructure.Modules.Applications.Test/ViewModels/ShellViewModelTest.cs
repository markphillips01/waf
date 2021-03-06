﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Waf.Applications;
using System.Waf.Applications.Services;
using System.Waf.UnitTesting;
using System.Waf.UnitTesting.Mocks;
using Test.InformationManager.Infrastructure.Modules.Applications.Views;
using Waf.InformationManager.Infrastructure.Interfaces.Applications;
using Waf.InformationManager.Infrastructure.Modules.Applications.Properties;
using Waf.InformationManager.Infrastructure.Modules.Applications.Services;
using Waf.InformationManager.Infrastructure.Modules.Applications.ViewModels;

namespace Test.InformationManager.Infrastructure.Modules.Applications.ViewModels
{
    [TestClass]
    public class ShellViewModelTest : InfrastructureTest
    {
        [TestMethod]
        public void ShowAndClose()
        {
            var messageService = Container.GetExportedValue<MockMessageService>();
            var shellView = Container.GetExportedValue<MockShellView>();
            var shellViewModel = Container.GetExportedValue<ShellViewModel>();

            // Show the ShellView
            Assert.IsFalse(shellView.IsVisible);
            shellViewModel.Show();
            Assert.IsTrue(shellView.IsVisible);

            // In this case it tries to get the title of the unit test framework which is ""
            Assert.AreEqual("", shellViewModel.Title);

            // Check the services
            Assert.AreEqual(Container.GetExportedValue<ShellService>(), shellViewModel.ShellService);
            Assert.AreEqual(Container.GetExportedValue<NavigationService>(), shellViewModel.NavigationService);

            // Show the About Dialog
            Assert.IsNull(messageService.Message);
            shellViewModel.AboutCommand.Execute(null);
            Assert.AreEqual(MessageType.Message, messageService.MessageType);
            Assert.IsNotNull(messageService.Message);

            // Close the ShellView via the ExitCommand
            shellViewModel.ExitCommand.Execute(null);
            Assert.IsFalse(shellView.IsVisible);
        }

        [TestMethod]
        public void ToolBarCommandsDelegation()
        {
            var shellView = Container.GetExportedValue<MockShellView>();
            var shellViewModel = Container.GetExportedValue<ShellViewModel>();

            var emptyCommand = new DelegateCommand(() => { });
            var newToolBarCommands = new ToolBarCommand[] 
            {
                new ToolBarCommand(emptyCommand, "Command 1"),
                new ToolBarCommand(emptyCommand, "Command 2")
            };

            Assert.IsFalse(shellView.ToolBarCommands.Any());
            shellViewModel.AddToolBarCommands(newToolBarCommands);
            AssertHelper.SequenceEqual(newToolBarCommands, shellView.ToolBarCommands);
            shellViewModel.ClearToolBarCommands();
            Assert.IsFalse(shellView.ToolBarCommands.Any());
        }

        [TestMethod]
        public void RestoreWindowLocationAndSize()
        {
            var shellView = Container.GetExportedValue<MockShellView>();
            shellView.VirtualScreenWidth = 1000;
            shellView.VirtualScreenHeight = 700;

            var settingsService = Container.GetExportedValue<ISettingsService>();
            var settings = settingsService.Get<AppSettings>();
            SetSettingsValues(settings, 20, 10, 400, 300, true);

            Container.GetExportedValue<ShellViewModel>();
            Assert.AreEqual(20, shellView.Left);
            Assert.AreEqual(10, shellView.Top);
            Assert.AreEqual(400, shellView.Width);
            Assert.AreEqual(300, shellView.Height);
            Assert.IsTrue(shellView.IsMaximized);

            shellView.Left = 25;
            shellView.Top = 15;
            shellView.Width = 450;
            shellView.Height = 350;
            shellView.IsMaximized = false;

            shellView.Close();
            AssertSettingsValues(settings, 25, 15, 450, 350, false);
        }

        [TestMethod]
        public void RestoreWindowLocationAndSizeSpecial()
        {
            var shellView = Container.GetExportedValue<MockShellView>();
            shellView.VirtualScreenWidth = 1000;
            shellView.VirtualScreenHeight = 700;

            var messageService = Container.GetExportedValue<IMessageService>();
            var shellService = Container.GetExportedValue<ShellService>();
            var navigationService = Container.GetExportedValue<NavigationService>();
            var settingsService = Container.GetExportedValue<ISettingsService>();
            var settings = settingsService.Get<AppSettings>();
            shellView.SetNAForLocationAndSize();

            SetSettingsValues(settings);
            new ShellViewModel(shellView, messageService, shellService, navigationService, settingsService).Close();
            AssertSettingsValues(settings, double.NaN, double.NaN, double.NaN, double.NaN, false);

            // Height is 0 => don't apply the Settings values
            SetSettingsValues(settings, 0, 0, 1, 0);
            new ShellViewModel(shellView, messageService, shellService, navigationService, settingsService).Close();
            AssertSettingsValues(settings, double.NaN, double.NaN, double.NaN, double.NaN, false);

            // Left = 100 + Width = 901 > VirtualScreenWidth = 1000 => don't apply the Settings values
            SetSettingsValues(settings, 100, 100, 901, 100);
            new ShellViewModel(shellView, messageService, shellService, navigationService, settingsService).Close();
            AssertSettingsValues(settings, double.NaN, double.NaN, double.NaN, double.NaN, false);

            // Top = 100 + Height = 601 > VirtualScreenWidth = 600 => don't apply the Settings values
            SetSettingsValues(settings, 100, 100, 100, 601);
            new ShellViewModel(shellView, messageService, shellService, navigationService, settingsService).Close();
            AssertSettingsValues(settings, double.NaN, double.NaN, double.NaN, double.NaN, false);

            // Use the limit values => apply the Settings values
            SetSettingsValues(settings, 0, 0, 1000, 700);
            new ShellViewModel(shellView, messageService, shellService, navigationService, settingsService).Close();
            AssertSettingsValues(settings, 0, 0, 1000, 700, false);
        }

        private void SetSettingsValues(AppSettings settings, double left = 0, double top = 0, double width = 0, double height = 0, bool isMaximized = false)
        {
            settings.Left = left;
            settings.Top = top;
            settings.Width = width;
            settings.Height = height;
            settings.IsMaximized = isMaximized;
        }

        private void AssertSettingsValues(AppSettings settings, double left, double top, double width, double height, bool isMaximized)
        {
            Assert.AreEqual(left, settings.Left);
            Assert.AreEqual(top, settings.Top);
            Assert.AreEqual(width, settings.Width);
            Assert.AreEqual(height, settings.Height);
            Assert.AreEqual(isMaximized, settings.IsMaximized);
        }
    }
}
