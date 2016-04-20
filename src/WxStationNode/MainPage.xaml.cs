using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace PervasiveDigital.Verdant.WxStationNode
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static TimeSpan Report1Interval = TimeSpan.FromMinutes(1.0);
        private static TimeSpan Report2Interval = TimeSpan.FromMinutes(1.5);

        private SuspendingEventHandler appSuspendEventHandler;
        private EventHandler<Object> appResumeEventHandler;

        private readonly ObservableCollection<DeviceListEntry> _listOfDevices;

        private readonly Dictionary<DeviceWatcher, String> _mapDeviceWatchersToDeviceSelector;
        private bool _watchersSuspended;
        private bool _watchersStarted;

        // Have all the devices enumerated by the device watcher?
        private bool _areAllDevicesEnumerated;

        private DispatcherTimer _r1Timer;
        private DispatcherTimer _r2Timer;

        private DeviceClient _deviceClient;

        public MainPage()
        {
            this.InitializeComponent();
            _listOfDevices = new ObservableCollection<DeviceListEntry>();

            _mapDeviceWatchersToDeviceSelector = new Dictionary<DeviceWatcher, String>();
            _watchersStarted = false;
            _watchersSuspended = false;

            _areAllDevicesEnumerated = false;

            _deviceClient = DeviceClient.Create(
                AzureIoT.HubUri, 
                AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey(AzureIoT.DeviceId, AzureIoT.PrimaryKey),
                TransportType.Http1);
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            // If we are connected to the device or planning to reconnect, we should disable the list of devices
            // to prevent the user from opening a device without explicitly closing or disabling the auto reconnect
            if (EventHandlerForDevice.Current.IsDeviceConnected
                || (EventHandlerForDevice.Current.IsEnabledAutoReconnect
                && EventHandlerForDevice.Current.DeviceInformation != null))
            {
                // These notifications will occur if we are waiting to reconnect to device when we start the page
                EventHandlerForDevice.Current.OnDeviceConnected = this.OnDeviceConnected;
                EventHandlerForDevice.Current.OnDeviceClose = this.OnDeviceClosing;
            }

            // Begin watching out for events
            StartHandlingAppEvents();

            // Initialize the desired device watchers so that we can watch for when devices are connected/removed
            InitializeDeviceWatchers();
            StartDeviceWatchers();

//            DeviceListSource.Source = listOfDevices;
        }

        /// <summary>
        /// Unregister from App events and DeviceWatcher events because this page will be unloaded.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected override void OnNavigatedFrom(NavigationEventArgs eventArgs)
        {
            StopDeviceWatchers();
            StopHandlingAppEvents();

            // We no longer care about the device being connected
            EventHandlerForDevice.Current.OnDeviceConnected = null;
            EventHandlerForDevice.Current.OnDeviceClose = null;
        }

        private void InitializeDeviceWatchers()
        {
            var acuRiteSelector = HidDevice.GetDeviceSelector(SuperMutt.Device.UsagePage, SuperMutt.Device.UsageId, SuperMutt.Device.Vid, SuperMutt.Device.Pid);

            // Create a device watcher to look for instances of the SuperMUTT device
            var acuRiteWatcher = DeviceInformation.CreateWatcher(acuRiteSelector);

            // Allow the EventHandlerForDevice to handle device watcher events that relates or effects our device (i.e. device removal, addition, app suspension/resume)
            AddDeviceWatcher(acuRiteWatcher, acuRiteSelector);
        }

        private void StartHandlingAppEvents()
        {
            appSuspendEventHandler = new SuspendingEventHandler(this.OnAppSuspension);
            appResumeEventHandler = new EventHandler<Object>(this.OnAppResume);

            // This event is raised when the app is exited and when the app is suspended
            App.Current.Suspending += appSuspendEventHandler;

            App.Current.Resuming += appResumeEventHandler;
        }

        private void StopHandlingAppEvents()
        {
            // This event is raised when the app is exited and when the app is suspended
            App.Current.Suspending -= appSuspendEventHandler;

            App.Current.Resuming -= appResumeEventHandler;
        }

        /// <summary>
        /// Registers for Added, Removed, and Enumerated events on the provided deviceWatcher before adding it to an internal list.
        /// </summary>
        /// <param name="deviceWatcher"></param>
        /// <param name="deviceSelector">The AQS used to create the device watcher</param>
        private void AddDeviceWatcher(DeviceWatcher deviceWatcher, String deviceSelector)
        {
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(this.OnDeviceAdded);
            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(this.OnDeviceRemoved);
            deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>(this.OnDeviceEnumerationComplete);

            _mapDeviceWatchersToDeviceSelector.Add(deviceWatcher, deviceSelector);
        }

        /// <summary>
        /// Starts all device watchers including ones that have been individually stopped.
        /// </summary>
        private void StartDeviceWatchers()
        {
            // Start all device watchers
            _watchersStarted = true;
            _areAllDevicesEnumerated = false;

            foreach (DeviceWatcher deviceWatcher in _mapDeviceWatchersToDeviceSelector.Keys)
            {
                if ((deviceWatcher.Status != DeviceWatcherStatus.Started)
                    && (deviceWatcher.Status != DeviceWatcherStatus.EnumerationCompleted))
                {
                    deviceWatcher.Start();
                }
            }
        }

        /// <summary>
        /// Stops all device watchers.
        /// </summary>
        private void StopDeviceWatchers()
        {
            // Stop all device watchers
            foreach (DeviceWatcher deviceWatcher in _mapDeviceWatchersToDeviceSelector.Keys)
            {
                if ((deviceWatcher.Status == DeviceWatcherStatus.Started)
                    || (deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted))
                {
                    deviceWatcher.Stop();
                }
            }

            // Clear the list of devices so we don't have potentially disconnected devices around
            ClearDeviceEntries();

            _watchersStarted = false;
        }

        /// <summary>
        /// Creates a DeviceListEntry for a device and adds it to the list of devices in the UI
        /// </summary>
        /// <param name="deviceInformation">DeviceInformation on the device to be added to the list</param>
        /// <param name="deviceSelector">The AQS used to find this device</param>
        private void AddDeviceToList(DeviceInformation deviceInformation, String deviceSelector)
        {
            // search the device list for a device with a matching interface ID
            var match = FindDevice(deviceInformation.Id);

            // Add the device if it's new
            if (match == null)
            {
                // Create a new element for this device interface, and queue up the query of its
                // device information
                match = new DeviceListEntry(deviceInformation, deviceSelector);

                // Add the new element to the end of the list of devices
                _listOfDevices.Add(match);
            }
        }

        private void RemoveDeviceFromList(String deviceId)
        {
            // Removes the device entry from the interal list; therefore the UI
            var deviceEntry = FindDevice(deviceId);

            _listOfDevices.Remove(deviceEntry);
        }

        private void ClearDeviceEntries()
        {
            _listOfDevices.Clear();
        }

        /// <summary>
        /// Searches through the existing list of devices for the first DeviceListEntry that has
        /// the specified device Id.
        /// </summary>
        /// <param name="deviceId">Id of the device that is being searched for</param>
        /// <returns>DeviceListEntry that has the provided Id; else a nullptr</returns>
        private DeviceListEntry FindDevice(String deviceId)
        {
            if (deviceId != null)
            {
                foreach (DeviceListEntry entry in _listOfDevices)
                {
                    if (entry.DeviceInformation.Id == deviceId)
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// We must stop the DeviceWatchers because device watchers will continue to raise events even if
        /// the app is in suspension, which is not desired (drains battery). We resume the device watcher once the app resumes again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnAppSuspension(Object sender, SuspendingEventArgs args)
        {
            if (_watchersStarted)
            {
                _watchersSuspended = true;
                StopDeviceWatchers();
            }
            else
            {
                _watchersSuspended = false;
            }
        }

        /// <summary>
        /// See OnAppSuspension for why we are starting the device watchers again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnAppResume(Object sender, Object args)
        {
            if (_watchersSuspended)
            {
                _watchersSuspended = false;
                StartDeviceWatchers();
            }
        }

        /// <summary>
        /// We will remove the device from the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInformationUpdate"></param>
        private async void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate deviceInformationUpdate)
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                new DispatchedHandler(() =>
                {
//                    rootPage.NotifyUser(deviceInformationUpdate.Id + " was removed.", NotifyType.StatusMessage);

                    RemoveDeviceFromList(deviceInformationUpdate.Id);
                }));
        }

        /// <summary>
        /// This function will add the device to the listOfDevices so that it shows up in the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInformation"></param>
        private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation deviceInformation)
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                new DispatchedHandler(() =>
                {
//                    rootPage.NotifyUser(deviceInformation.Id + " was added.", NotifyType.StatusMessage);

                    AddDeviceToList(deviceInformation, _mapDeviceWatchersToDeviceSelector[sender]);
                }));
        }

        /// <summary>
        /// Notify the UI whether or not we are connected to a device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void OnDeviceEnumerationComplete(DeviceWatcher sender, Object args)
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                new DispatchedHandler(async () =>
                {
                    _areAllDevicesEnumerated = true;

                    // If we finished enumerating devices and the device has not been connected yet, the OnDeviceConnected method
                    // is responsible for selecting the device in the device list (UI); otherwise, this method does that.
                    if (EventHandlerForDevice.Current.IsDeviceConnected)
                    {
                        //SelectDeviceInList(EventHandlerForDevice.Current.DeviceInformation.Id);

                        //ButtonDisconnectFromDevice.Content = ButtonNameDisconnectFromDevice;

                        //rootPage.NotifyUser("Currently connected to: " + EventHandlerForDevice.Current.DeviceInformation.Id, NotifyType.StatusMessage);
                    }
                    else if (EventHandlerForDevice.Current.IsEnabledAutoReconnect && EventHandlerForDevice.Current.DeviceInformation != null)
                    {
                        //// We will be reconnecting to a device
                        //ButtonDisconnectFromDevice.Content = ButtonNameDisableReconnectToDevice;

                        //rootPage.NotifyUser("Waiting to reconnect to device: " + EventHandlerForDevice.Current.DeviceInformation.Id, NotifyType.StatusMessage);
                    }
                    else
                    {
                        var entry = _listOfDevices[0];
                        
                        // Create an EventHandlerForDevice to watch for the device we are connecting to
                        EventHandlerForDevice.CreateNewEventHandlerForDevice();

                        // Get notified when the device was successfully connected to or about to be closed
                        EventHandlerForDevice.Current.OnDeviceConnected = this.OnDeviceConnected;
                        EventHandlerForDevice.Current.OnDeviceClose = this.OnDeviceClosing;

                        // It is important that the FromIdAsync call is made on the UI thread because the consent prompt can only be displayed
                        // on the UI thread. Since this method is invoked by the UI, we are already in the UI thread.
                        var openSuccess = await EventHandlerForDevice.Current.OpenDeviceAsync(entry.DeviceInformation, entry.DeviceSelector);
                    }
                }));
        }

        private async void R1TimerOnTick(object sender, object o)
        {
            try
            {
                var inputReport = await EventHandlerForDevice.Current.Device.GetInputReportAsync(1);
                var data = inputReport.Data.ToArray();
                var report = AcuRite.AcuriteParser.ParseReport(data);
                if (report != null)
                {
                    var messageString = JsonConvert.SerializeObject(report);
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));
                    await _deviceClient.SendEventAsync(message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async void R2TimerOnTick(object sender, object o)
        {
            try
            {
                var inputReport = await EventHandlerForDevice.Current.Device.GetInputReportAsync(2);
                var data = inputReport.Data.ToArray();
                var report = AcuRite.AcuriteParser.ParseReport(data);
                if (report != null)
                {
                    var messageString = JsonConvert.SerializeObject(report);
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));
                    await _deviceClient.SendEventAsync(message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// If all the devices have been enumerated, select the device in the list we connected to. Otherwise let the EnumerationComplete event
        /// from the device watcher handle the device selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInformation"></param>
        private void OnDeviceConnected(EventHandlerForDevice sender, DeviceInformation deviceInformation)
        {
            // Find and select our connected device
            if (_areAllDevicesEnumerated)
            {
                if (_r1Timer == null)
                {
                    _r1Timer = new DispatcherTimer();
                    _r1Timer.Interval = Report1Interval;
                    _r1Timer.Tick += R1TimerOnTick;
                }
                if (_r2Timer == null)
                {
                    _r2Timer = new DispatcherTimer();
                    _r2Timer.Interval = Report2Interval;
                    _r2Timer.Tick += R2TimerOnTick;
                }
                _r1Timer.Start();
                _r2Timer.Start();
            }
        }

        /// <summary>
        /// The device was closed. If we will autoreconnect to the device, reflect that in the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInformation"></param>
        private async void OnDeviceClosing(EventHandlerForDevice sender, DeviceInformation deviceInformation)
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                new DispatchedHandler(() =>
                {
                    _r1Timer?.Stop();
                    _r2Timer?.Stop();

                    //// We were connected to the device that was unplugged, so change the "Disconnect from device" button
                    //// to "Do not reconnect to device"
                    //if (ButtonDisconnectFromDevice.IsEnabled && EventHandlerForDevice.Current.IsEnabledAutoReconnect)
                    //{
                    //    ButtonDisconnectFromDevice.Content = ButtonNameDisableReconnectToDevice;
                    //}
                }));
        }
    }
}
