#region Copyright
// ****************************************************************************
// <copyright file="NavigationService.cs">
// Copyright � Vyacheslav Volkov 2012-2014
// </copyright>
// ****************************************************************************
// <author>Vyacheslav Volkov</author>
// <email>vvs0205@outlook.com</email>
// <project>MugenMvvmToolkit</project>
// <web>https://github.com/MugenMvvmToolkit/MugenMvvmToolkit</web>
// <license>
// See license.txt in this solution or http://opensource.org/licenses/MS-PL
// </license>
// ****************************************************************************
#endregion
using System;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using JetBrains.Annotations;
using MugenMvvmToolkit.Binding;
using MugenMvvmToolkit.Binding.Core;
using MugenMvvmToolkit.Interfaces;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Interfaces.Navigation;
using MugenMvvmToolkit.Interfaces.ViewModels;
using MugenMvvmToolkit.Models;
using MugenMvvmToolkit.Models.EventArg;
using MugenMvvmToolkit.Views.Activities;

namespace MugenMvvmToolkit.Infrastructure.Navigation
{
    public class NavigationService : INavigationService
    {
        #region Fields

        private const string ParameterSerializer = "viewmodelparameterdata";
        private const string ParameterString = "viewmodelparameter";
        private const string FirstActivityKey = "@%$#first";

        private readonly ISerializer _serializer;
        private Activity _currentActivity;
        private Intent _prevIntent;
        private bool _isNew;
        private bool _isBack;
        private object _parameter;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationService"/> class.
        /// </summary>
        public NavigationService([NotNull] ISerializer serializer)
        {
            Should.NotBeNull(serializer, "serializer");
            _serializer = serializer;
        }

        #endregion

        #region Methods

        private bool GoBackInternal()
        {
            if (!TryPopBackStackImmediate(_currentActivity) ||
                !RaiseNavigating(new NavigatingCancelEventArgs(_prevIntent, NavigationMode.Back,
                    GetParameterFromIntent(_prevIntent))))
                return false;

            _isBack = true;
            _currentActivity.Finish();
            //NOTE: If it's the first activity, we need to raise the back navigation event.
            var mainActivity = _currentActivity.Intent.HasExtra(FirstActivityKey);
            if (mainActivity || (_prevIntent == null && IsMainLauncher(_currentActivity)))
            {
                RaiseNavigated(null, NavigationMode.Back, null);
                //Trying to dispose main view model.
                if (!mainActivity)
                {
                    var viewModel = BindingServiceProvider
                        .ContextManager
                        .GetBindingContext(_currentActivity)
                        .Value as IViewModel;
                    if (viewModel != null)
                        viewModel.Dispose();
                }
                _currentActivity = null;
            }
            return true;
        }

        private static bool TryPopBackStackImmediate(Activity activity)
        {
#if API8
            return true;
#else
            if (activity == null)
                return true;
            var fragmentManager = activity.TryGetFragmentManager();
            if (fragmentManager == null)
                return true;
            return !fragmentManager.PopBackStackImmediate();
#endif
        }

        private bool RaiseNavigating(NavigatingCancelEventArgs args)
        {
            var handler = Navigating;
            if (handler != null)
            {
                handler(this, args);
                return !args.Cancel;
            }
            return true;
        }

        private static bool IsMainLauncher(Activity activity)
        {
            var attr = activity.GetType()
                .GetCustomAttributes(typeof(ActivityAttribute), false)
                .OfType<ActivityAttribute>()
                .FirstOrDefault();
            return attr != null && attr.MainLauncher;
        }

        private void RaiseNavigated(object content, NavigationMode mode, object parameter)
        {
            var handler = Navigated;
            if (handler != null)
                handler(this, new NavigationEventArgs(content, parameter, mode));
        }

        private object GetParameterFromIntent(Intent intent)
        {
            if (intent == null)
                return null;
            var value = intent.GetStringExtra(ParameterString);
            if (value != null)
                return value;
            var bytes = intent.GetByteArrayExtra(ParameterSerializer);
            if (bytes == null)
                return null;
            using (var ms = new MemoryStream(bytes))
                return _serializer.Deserialize(ms);
        }

        #endregion

        #region Implementation of INavigationService

        /// <summary>
        ///     Indicates whether the navigator can navigate back.
        /// </summary>
        public virtual bool CanGoBack
        {
            get { return _currentActivity != null; }
        }

        /// <summary>
        ///     Indicates whether the navigator can navigate forward.
        /// </summary>
        public virtual bool CanGoForward
        {
            get { return false; }
        }

        /// <summary>
        ///     The current content.
        /// </summary>
        public virtual object CurrentContent
        {
            get { return _currentActivity; }
        }

        /// <summary>
        ///     Navigates back.
        /// </summary>
        public virtual void GoBack()
        {
            GoBackInternal();
        }

        /// <summary>
        ///     Navigates forward.
        /// </summary>
        public virtual void GoForward()
        {
            Should.MethodBeSupported(false, "GoForward()");
        }

        /// <summary>
        ///     Raised as part of the activity lifecycle when an activity is going into the background.
        /// </summary>
        public virtual void OnPauseActivity(Activity activity, IDataContext context = null)
        {
            if (_isNew || _isBack || !ReferenceEquals(activity, _currentActivity))
                return;
            RaiseNavigating(new NavigatingCancelEventArgs());
            RaiseNavigated(null, NavigationMode.New, null);
            _currentActivity = null;
        }

        /// <summary>
        ///     Called when the activity had been stopped, but is now again being displayed to the user.
        /// </summary>
        public virtual void OnStartActivity(Activity activity, IDataContext context = null)
        {
            if (ReferenceEquals(activity, _currentActivity))
                return;
            _currentActivity = activity;
            if (_isNew)
            {
                RaiseNavigated(activity, NavigationMode.New, _parameter);
                _isNew = false;
                _parameter = null;
            }
            else
            {
                RaiseNavigated(activity, NavigationMode.Back, GetParameterFromIntent(activity.Intent));
                _isBack = false;
            }
        }

        /// <summary>
        ///     Called when the activity is starting.
        /// </summary>
        public void OnCreateActivity(Activity activity, IDataContext context = null)
        {
            OnStartActivity(activity);
        }

        /// <summary>
        /// Gets a navigation parameter from event args.
        /// </summary>
        public virtual object GetParameterFromArgs(EventArgs args)
        {
            var cancelArgs = args as NavigatingCancelEventArgs;
            if (cancelArgs == null)
                return ((NavigationEventArgs)args).Parameter;
            return cancelArgs.Parameter;
        }

        /// <summary>
        ///     Navigates using cancel event args.
        /// </summary>
        public virtual bool Navigate(NavigatingCancelEventArgsBase args)
        {
            if (!args.IsCancelable)
                return false;
            var eventArgs = ((NavigatingCancelEventArgs)args);
            if (eventArgs.NavigationMode != NavigationMode.Back && eventArgs.Intent == null)
                return Navigate(eventArgs.Mapping, eventArgs.Parameter, null);
            return GoBackInternal();
        }

        /// <summary>
        ///     Displays the content located at the specified <see cref="IViewMappingItem" />.
        /// </summary>
        /// <param name="source">
        ///     The <c>IViewPageMappingItem</c> of the content to display.
        /// </param>
        /// <param name="parameter">
        ///     A <see cref="T:System.Object" /> that contains data to be used for processing during
        ///     navigation.
        /// </param>
        /// <param name="dataContext">
        ///     The specified <see cref="IDataContext" />.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the content was successfully displayed; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool Navigate(IViewMappingItem source, object parameter, IDataContext dataContext)
        {
            Should.NotBeNull(source, "source");
            if (!RaiseNavigating(new NavigatingCancelEventArgs(source, NavigationMode.New, parameter)))
                return false;
            bool isFirstActivity = _currentActivity == null;
            Context context;
            var activity = _currentActivity ?? SplashScreenActivityBase.Current;
            if (activity == null)
                context = Application.Context;
            else
            {
                var attr = activity.GetType()
                                   .GetCustomAttributes(typeof(ActivityAttribute), false)
                                   .OfType<ActivityAttribute>()
                                   .FirstOrDefault();
                if (attr == null || !attr.NoHistory)
                    _prevIntent = activity.Intent;
                context = activity;
            }

            var intent = new Intent(context, source.ViewType);
            if (activity == null)
                intent.AddFlags(ActivityFlags.NewTask);
            if (isFirstActivity)
                intent.PutExtra(FirstActivityKey, true);
            if (parameter != null)
            {
                var s = parameter as string;
                if (s == null)
                {
                    using (var stream = _serializer.Serialize(parameter))
                        intent.PutExtra(ParameterSerializer, stream.ToArray());
                }
                else
                    intent.PutExtra(ParameterString, s);
            }
            _isNew = true;
            _parameter = parameter;
            context.StartActivity(intent);
            return true;
        }

        /// <summary>
        ///     Raised prior to navigation.
        /// </summary>
        public virtual event EventHandler<INavigationService, NavigatingCancelEventArgsBase> Navigating;

        /// <summary>
        ///     Raised after navigation.
        /// </summary>
        public virtual event EventHandler<INavigationService, NavigationEventArgsBase> Navigated;

        #endregion
    }
}