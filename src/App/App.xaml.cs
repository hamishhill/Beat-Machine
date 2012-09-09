using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Threading;
using BeatMachine.EchoNest;
using System.ComponentModel;
using BeatMachine.Model;
using NLog;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Xna.Framework;

namespace BeatMachine
{
    public partial class App : Application
    {
        private static Logger logger;

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

            SmartDispatcher.Initialize(Deployment.Current.Dispatcher);
            DispatcherHelper.Initialize();


            logger = LogManager.GetCurrentClassLogger();
            
            Model = new DataModel();

            // Create the database if it does not exist.
            using (BeatMachineDataContext db =
                new BeatMachineDataContext(BeatMachineDataContext.DBConnectionString))
            {
                if (!db.DatabaseExists())
                {
                    logger.Info("Creating new database");
                    db.CreateDatabase();
                }
                else
                {
                    logger.Info("Database exists already");
                }
            }

            // To make MediaPlayer.Play work according to VS error link
            this.ApplicationLifetimeObjects.Add(new XNAAsyncDispatcher(TimeSpan.FromMilliseconds(50)));

        }

        public Model.DataModel Model
        {
            get;
            set;
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            Model.Initialize();

            // Set up workflow through events on the model
            Model.PropertyChanged += new PropertyChangedEventHandler(
                (s, ev) =>
                {
                    Model.RunWorkflow(ev.PropertyName);
                });
            Model.RunWorkflow(null);
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            logger.Info("Application activated");
            if (!e.IsApplicationInstancePreserved)
            {
                logger.Info("Instance was not preserved");

                IDictionary<string, object> state =
                    PhoneApplicationService.Current.State;

                if (state.ContainsKey("Model"))
                {
                    Model = (DataModel)state["Model"];
                }

                // Set up workflow through events on the model
                Model.PropertyChanged += new PropertyChangedEventHandler(
                    (s, ev) =>
                    {
                        Model.RunWorkflow(ev.PropertyName);
                    });
                Model.RunWorkflow(null);
            }
            else
            {
                logger.Info("Instance was preserved");
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            IDictionary<string, object> state =
                 PhoneApplicationService.Current.State;
            state["Model"] = Model;
            logger.Info("Application deactivated");
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion

        private void PlayApplicationBarButton_Click(object sender, EventArgs e)
        {
            RootFrame.Navigate(new Uri("/View/Play.xaml", UriKind.RelativeOrAbsolute));
        }

        private void SongsApplicationBarButton_Click(object sender, EventArgs e)
        {
            RootFrame.Navigate(new Uri("/View/Songs.xaml", UriKind.RelativeOrAbsolute));
        }

        private void SettingsApplicationBarButton_Click(object sender, EventArgs e)
        {
            RootFrame.Navigate(new Uri("/View/Settings.xaml", UriKind.RelativeOrAbsolute));
        }
    }

    public class XNAAsyncDispatcher : IApplicationService
    {
        private DispatcherTimer frameworkDispatcherTimer;

        public XNAAsyncDispatcher(TimeSpan dispatchInterval) { this.frameworkDispatcherTimer = new DispatcherTimer();
            this.frameworkDispatcherTimer.Tick += new EventHandler(frameworkDispatcherTimer_Tick);
            this.frameworkDispatcherTimer.Interval = dispatchInterval;
        }

        void IApplicationService.StartService(ApplicationServiceContext context) { this.frameworkDispatcherTimer.Start(); }   
        void IApplicationService.StopService() { this.frameworkDispatcherTimer.Stop(); }   
        void frameworkDispatcherTimer_Tick(object sender, EventArgs e) { FrameworkDispatcher.Update(); }
    }
}